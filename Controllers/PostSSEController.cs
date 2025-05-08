using Microsoft.AspNetCore.Mvc;
using SHAW.Controllers.Util;
using SHAW.DataAccess.Util;
using SHAW.DataAccess.Models;
using StarFederation.Datastar.DependencyInjection;
using System.Text.Json;

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

        var json = JsonSerializer.Serialize(postsFormatted);

        await _sse.MergeFragmentsAsync($@"
            <div id=""main-left"" class=""col-md-8"">
                <div id=""title-button-container""
                     class=""d-flex justify-content-between align-items-center mb-4""
                >
                    <h1 class=""mb-4"">Community Forum</h1>
                    <button class=""btn btn-primary""
                            data-on-click=""@get('forum/posts/create_view')""
                    >
                        <i class=""bi bi-plus-circle""></i>
                        Add Post
                    </button>
                </div>
        <div 
            id='posts'
            data-signals-posts='{json}'>
            <forum-posts
                title='Community Posts'
                data-attr-posts='$posts'
                data-on-post-selected='$selected = evt.detail.value; @get(""/forum/view"")'>
            </forum-posts>
        </div>
        </div>
    ");
    }

    [HttpGet("posts/create_view")]
    public async Task CreatePostsView()
    {
        var controller = new DataAccess.Controllers.UserController(
            DbConnectionFactory.CreateDbConnection(_env)
        );
        var cookieExists = Request.Cookies.TryGetValue("loginKey", out string? key);
        if (!cookieExists || string.IsNullOrEmpty(key))
        {
            return;
        }

        await _sse.MergeFragmentsAsync($@"
    <div id=""main-left"" class=""col-md-8"">
        <div id=""title-button-container"" class=""d-flex justify-content-between align-items-center mb-4"">
            <h1 class=""mb-4"">Community Forum</h1>
        </div>
        <div class=""card"">
            <div class=""card-header"">
                <h5 class=""card-title mb-0"">Create New Post</h5>
            </div>
            <div class=""card-body"">
                <form id=""newPostForm"">
                    <div class=""mb-3"">
                        <label for=""postTitle"" class=""form-label"">Title</label>
                        <input type=""text"" 
                               class=""form-control"" 
                               id=""postTitle"" 
                               name=""title"" 
                               required
                               placeholder=""Enter post title"">
                    </div>
                    <div class=""mb-3"">
                        <label for=""postBody"" class=""form-label"">Content</label>
                        <textarea class=""form-control"" 
                                  id=""postBody"" 
                                  name=""body"" 
                                  rows=""6"" 
                                  required
                                  placeholder=""Write your post content here...""></textarea>
                    </div>
                    <div class=""d-flex justify-content-between"">
                        <button type=""button"" 
                                class=""btn btn-secondary"" 
                                data-on-click=""@get('/forum/posts')"">
                            Cancel
                        </button>
                        <button class=""btn btn-primary"" data-on-click=""@get('/forum/posts/create', {{contentType: 'form'}})"">
                            Create Post
                        </button>
                    </div>
                </form>
            </div>
        </div>
    </div>
");
    }

    [HttpGet("posts/create")]
    public async Task CreatePosts([FromQuery] string title, [FromQuery] string body)
    {
        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(body))
        {
            await _sse.ExecuteScriptAsync("console.log('Title and body cannot be empty')");
            return;
        }

        // Verify counselor permission first
        var userController = new DataAccess.Controllers.UserController(
            DbConnectionFactory.CreateDbConnection(_env)
        );

        var cookieExists = Request.Cookies.TryGetValue("loginKey", out string? key);
        if (!cookieExists || string.IsNullOrEmpty(key))
        {
            await _sse.ExecuteScriptAsync("console.log('Unauthorized')");
            return;
        }

        try
        {
            // Get user ID from the login key
            var userId = await userController.GetUserIdFromLoginKey(key);
            if (userId == null)
            {
                await _sse.ExecuteScriptAsync("console.log('Failed to get user ID')");
                return;
            }

            await WithPostController(async controller =>
            {
                await controller.CreatePost(new PostModel
                {
                    Title = title,
                    Body = body,
                    CreatedOn = DateTime.UtcNow,
                    UserId = userId.Value,
                    Author = ""
                });
                return true;
            });

            await _sse.ExecuteScriptAsync("setTimeout(() => window.location.href = '/forum')");
        }
        catch (Exception e)
        {
            await _sse.ExecuteScriptAsync($"console.log('Error creating post: {e.Message}')");
            await _sse.MergeFragmentsAsync(@"
            <div id=""main-left"" class=""col-md-8"">
                <div class=""alert alert-danger"">
                    Failed to create post. Please try again.
                </div>
                    <a href=""forum"" class=""btn btn-primary mt-3"">Return to Forum</a>
            </div>
        ");
        }
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
                <div class=""row justify-content-center"">
                    <div class=""col-lg-10 col-xl-8"">
                        <nav aria-label=""breadcrumb"">
                            <ol class=""breadcrumb"">
                                <li class=""breadcrumb-item""><a href=""forum"">Forum</a></li>
                                <li class=""breadcrumb-item active"" aria-current=""page"">{selected.title}</li>
                            </ol>
                        </nav>
                        <div class=""card mb-4"" style=""width: 100%; height: 90%;"">
                            <div class=""card-body"">
                                <h1 class=""card-title"">{selected.title}</h1>
                                <div class=""text-muted mb-3"">
                                    Posted by {selected.author} on {selected.date}
                                </div>
                                <div class=""mb-3"">
                                    {selected.body}
                                </div>
                                <div 
                                    class=""mt-2 d-flex gap-2 align-items-center""
                                    data-signals=""{{interactError: '', likes: '{selected.likes}', dislikes: '{selected.dislikes}'}}""
                                >
                                    <button data-on-click='@post(""/forum/interact?postId={selected.id}&action=1"")' class=""btn btn-sm btn-outline-success like-btn"">👍</button>
                                    <span data-text='$likes'></span>
                                    <button data-on-click='@post(""/forum/interact?postId={selected.id}&action=0"")' class=""btn btn-sm btn-outline-danger dislike-btn"">👎</button>
                                    <span data-text='$dislikes'></span>
                                    <div class=""mt-2"" style=""color: red;"" data-text=""'* ' + $interactError"" data-show=""$interactError != ''""></div>
                                </div> 
                                <div
                                    class=""mt-3"" 
                                    id='comments'
                                    data-on-load='@get(""/forum/comments?postId={selected.id}"")'
                                    data-signals=""{{comments: []}}""
                                >
                                    <post-comments
                                        data-attr-comments='$comments'
                                    </post-comments>
                                </div>
                                <div class=""mb-2 mt-3 d-flex gap-2"" data-signals=""{{comment: ''}}"">
                                    <input 
                                        type=""text"" 
                                        data-bind='comment'
                                        class=""form-control"" 
                                        placeholder=""Enter comment here""
                                    >
                                    <button 
                                        class=""btn btn-success""
                                        data-on-click='@post(""/forum/comment/add?postId={selected.id}"")'
                                    >
                                        +
                                    </button>
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

    [HttpPost("comment/add")]
    public async Task AddComment()
    {
        if(!Request.Query.TryGetValue("postid", out var pid) || !RequestUtil.TryGetLoginKey(Request, out string key))
        {
            return;
        }

        int postId;
        try
        {
            string comment = (await SignalUtil.GetModelFromSignal<AddCommentModel>(_reader)).comment;
            if(string.IsNullOrEmpty(comment))
            {
                throw new Exception();
            }

            postId = int.Parse(pid.ToString());
            using(var u = CreateUserDbController())
            using(var p = CreatePostDbController())
            {
                int uid = (await u.TryGetUser(key)).Id;
                await p.AddComment(postId, uid, comment);
            }

            await _sse.MergeSignalsAsync("{comment: ''}");
        }
        catch
        {
            return;
        }

        await GetComments();
    }

    [HttpGet("comments")]
    public async Task GetComments()
    {
        if(!Request.Query.TryGetValue("postid", out var pid))
        {
            return;
        }

        int postId;
        try
        {
            postId = int.Parse(pid.ToString());
            List<CommentModel> comments = [];
            using(var p = CreatePostDbController())
            {
                comments = await p.GetComments(postId);
            }

            var commentsFormatted = comments.Select(x => new {
                id = x.Id,
                text = x.Content,
                date = x.CreatedOn.ToString("yyyy-MM-dd"),
                username = x.Username
            });
            
            var json = JsonSerializer.Serialize(commentsFormatted);

            await _sse.MergeSignalsAsync($"{{comments: {json}}}");
        }
        catch 
        {
            Console.WriteLine("failed to get comments");
            return;
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

public class PostCreateModel
{
    public required string Title { get; set; }
    public required string Body { get; set; }
}

public class AddCommentModel
{
    public required string comment { get; set; }
}
