using Microsoft.AspNetCore.Mvc;
using SHAW.Controllers.Util;
using SHAW.DataAccess.Util;
using StarFederation.Datastar.DependencyInjection;

namespace SHAW.Controllers;

[Route("forum")]
public class PostSSEController : ControllerBase
{
    private IDatastarServerSentEventService _sse;
    private IDatastarSignalsReaderService _reader;
    private IHostEnvironment _env;

    private DataAccess.Controllers.PostController CreatePostDbController() =>
        new DataAccess.Controllers.PostController(DbConnectionFactory.CreateDbConnection(_env));

    private DataAccess.Controllers.UserController CreateUserDbController()
    {
        return new DataAccess.Controllers.UserController(DbConnectionFactory.CreateDbConnection(_env));
    }

    private async Task<T> WithPostController<T>(Func<DataAccess.Controllers.PostController, Task<T>> action)
    {
        using var controller = new DataAccess.Controllers.PostController(
            DbConnectionFactory.CreateDbConnection(_env));
        return await action(controller);
    }


    public PostSSEController(IDatastarServerSentEventService sse, IDatastarSignalsReaderService reader,
        IHostEnvironment env)
    {
        _sse = sse;
        _reader = reader;
        _env = env;
    }

    [HttpGet("posts")]
    public async Task ForumPosts()
    {
        var posts = await WithPostController(controller => controller.GetPosts());
        
        var postsFormatted = posts.Select(p => new
        {
            id = p.Id,
            title = p.Title,
            author = p.Author,
            date = p.CreatedOn.ToString("yyyy-MM-dd"),
            body = p.Body,
            likes = p.Likes,
            dislikes = p.Dislikes
        });

        var json = System.Text.Json.JsonSerializer.Serialize(postsFormatted);

        await _sse.MergeFragmentsAsync($@"
        <div 
            id='posts'
            data-signals-posts='{json}'>
            <forum-posts
                title='Community Posts'
                data-attr-posts='$posts'
                data-on-post-selected='$selected = evt.detail.value; @get(""/forum/view"")'>
            </forum-posts>
        </div>
    ");
    }

    [HttpGet("view")]
    public async Task ViewPost()
    {
        try
        {
            var forumData = await SignalUtil.GetModelFromSignal<PostViewSignalModel>(_reader);
            
            if (forumData?.selected == null)
            {
                await _sse.MergeFragmentsAsync(@"
                <div id=""post-view"">
                    <div class=""container my-5 text-center"">
                        <h2>Post not found</h2>
                        <a href=""forum"" class=""btn btn-primary mt-3"">Return to Forum</a>
                    </div>
                </div>");
                return;
            }

            var selected = forumData.selected;

            await _sse.MergeFragmentsAsync($@"
            <div id=""post-view"">
                <div class=""container my-5"">
                    <div class=""row"">
                        <div class=""col-md-8"">
                            <nav aria-label=""breadcrumb"">
                                <ol class=""breadcrumb"">
                                    <li class=""breadcrumb-item""><a href=""forum"">Forum</a></li>
                                    <li class=""breadcrumb-item active"" aria-current=""page"">{selected.title}</li>
                                </ol>
                            </nav>
                            <div class=""card"">
                                <div class=""card-body"">
                                    <h1 class=""card-title"">{selected.title}</h1>
                                    <div class=""text-muted mb-3"">
                                        Posted by {selected.author} on {selected.date}
                                    </div>
                                    <p class=""card-text"">{selected.body}</p>
                                    <div 
                                        class=""mt-2 d-flex gap-2 align-items-center""
                                        data-signals=""{{interactError: '', likes: '{selected.likes}', dislikes: '{selected.dislikes}'}}""
                                    >
                                        <button data-on-click='@post(""/forum/interact?postId={selected.id}&action=1"")' class=""btn btn-sm btn-outline-success like-btn"">👍</button>
                                        <span data-text='$likes'></span>
                                        <button data-on-click='@post(""/forum/interact?postId={selected.id}&action=0"")' class=""btn btn-sm btn-outline-danger dislike-btn"">👎</button>
                                        <span data-text='$dislikes'></span>
                                    </div> 
                                    <div class=""mt-2"" style=""color: red;"" data-text=""'* ' + $interactError"" data-show=""$interactError != ''""></div>
                                </div>
                            </div>
                        </div>
                        <div class=""col-md-4"">
                            <div class=""card"">
                                <div class=""card-body"">
                                    <h5 class=""card-title"">Discussion Guidelines</h5>
                                    <ul class=""list-unstyled"">
                                        <li class=""mb-2"">✓ Be respectful and supportive</li>
                                        <li class=""mb-2"">✓ Stay on topic</li>
                                        <li class=""mb-2"">✓ Share constructive feedback</li>
                                    </ul>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            await _sse.MergeFragmentsAsync(@"
            <div id=""post-view"">
                <div class=""container my-5 text-center"">
                    <h2>Error loading post</h2>
                    <p>An error occurred while loading the post.</p>
                    <a href=""forum"" class=""btn btn-primary mt-3"">Return to Forum</a>
                </div>
            </div>");
        }
    }

    [HttpPost("interact")]
    public async Task Interact()
    {
        if(
            !Request.Query.TryGetValue("postid", out var pid)
            || !Request.Query.TryGetValue("action", out var a)
            || !RequestUtil.TryGetLoginKey(Request, out string key)
        ) 
        {
            await _sse.MergeSignalsAsync("{interactError: 'Server error'}");
            return;
        }

        int postId;
        bool action;
        try
        {
            postId = int.Parse(pid.ToString());
            action = int.Parse(a.ToString()) == 1;
        }
        catch
        {
            await _sse.MergeSignalsAsync("{interactError: 'Server error'}");
            return;
        }

        try
        {
            using(var u = CreateUserDbController())
            using(var c = CreatePostDbController())
            {
                int uid = (await u.TryGetUser(key)).Id;
                await c.TryInteract(postId, uid, action);
                // refresh likes/dislikes
                int likes = await c.GetLikes(postId);
                int dislikes = await c.GetDislikes(postId);
                
                await _sse.MergeSignalsAsync($"{{likes: '{likes}', dislikes: '{dislikes}'}}");
            }
        }
        catch
        {
            await _sse.MergeSignalsAsync($"{{interactError: 'Failed to {(action ? "like" : "dislike")}}}");
        }

    }
}