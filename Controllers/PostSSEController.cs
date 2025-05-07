using System.Data.Common;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using SHAW.Controllers.Util;
using SHAW.DataAccess.Models;
using SHAW.DataAccess.Util;
using StarFederation.Datastar.DependencyInjection;
using StarFederation.Datastar.ModelBinding;

namespace SHAW.Controllers;

[Route("forum")]
public class PostSSEController : ControllerBase
{
    private IDatastarServerSentEventService _sse;
    private IDatastarSignalsReaderService _reader;
    private IHostEnvironment _env;

    private DataAccess.Controllers.PostController CreatePostDbController() =>
        new DataAccess.Controllers.PostController(DbConnectionFactory.CreateDbConnection(_env));

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
            body = p.Body
        });

        var json = System.Text.Json.JsonSerializer.Serialize(postsFormatted);

        await _sse.MergeFragmentsAsync($@"
            <div id=""main-left"" class=""col-md-8"">
                <div id=""title-button-container""
                     class=""d-flex justify-content-between align-items-center mb-4""
                     data-on-load=""@get('user/isCounselor')""
                >
                    <h1 class=""mb-4"">Community Forum</h1>
                    <button data-show=""$isCounselor"" class=""btn btn-primary""
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
        if (!cookieExists || string.IsNullOrEmpty(key) || !await controller.IsCounselor(key))
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
            _sse.ExecuteScriptAsync("console.log('Title and body cannot be empty')");
            return;
        }

        // Verify counselor permission first
        var userController = new DataAccess.Controllers.UserController(
            DbConnectionFactory.CreateDbConnection(_env)
        );

        var cookieExists = Request.Cookies.TryGetValue("loginKey", out string? key);
        if (!cookieExists || string.IsNullOrEmpty(key) || !await userController.IsCounselor(key))
        {
            _sse.ExecuteScriptAsync("console.log('Unauthorized: Only counselors can create posts')");
            return;
        }

        try
        {
            // Get user ID from the login key
            var userId = await userController.GetUserIdFromLoginKey(key);
            if (userId == null)
            {
                _sse.ExecuteScriptAsync("console.log('Failed to get user ID')");
                return;
            }

            await WithPostController(async controller =>
            {
                await controller.CreatePost(new PostsModel
                {
                    Title = title,
                    Body = body,
                    CreatedOn = DateTime.UtcNow,
                    UserId = userId.Value
                });
                return true;
            });

            await _sse.ExecuteScriptAsync("setTimeout(() => window.location.href = '/forum')");
        }
        catch (Exception e)
        {
            _sse.ExecuteScriptAsync($"console.log('Error creating post: {e.Message}')");
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
}

public class PostCreateModel
{
    public string Title { get; set; }
    public string Body { get; set; }
}