/**
 * ForumPosts Web Component
 * 
 * @description Displays a list of forum posts that users can click to select
 * 
 * @property {string} title - Optional title to display at the top of the posts container
 * @property {string} posts - JSON string of posts array with the following structure:
 * 
 * Expected JSON Schema for posts:
 * [
 *   {
 *     "id": number,          // Unique identifier for the post
 *     "title": string,       // Post title to display
 *     "author": string,      // Username of post author
 *     "date": string,        // Date of post (optional, default "Just now")
 *     "body": string         // Content of the post (not displayed in the list view)
 *   },
 *   ...
 * ]
 * 
 * @fires post-selected - Custom event when user clicks on a post
 * @property {Object} event.detail.value - The selected post object
 */
class ForumPosts extends HTMLElement {
    constructor() {
        super();
        this._posts = [];
        this.attachShadow({ mode: 'open' });
        // Bind the click handler to this instance
        this._handleClick = this._handleClick.bind(this);
    }

    static get observedAttributes() {
        return ['posts', 'title'];
    }

    connectedCallback() {
        this.shadowRoot.addEventListener('click', this._handleClick);
    }

    disconnectedCallback() {
        this.shadowRoot.removeEventListener('click', this._handleClick);
    }

    _handleClick(e) {
        const postItem = e.target.closest('.post-item');
        if (postItem) {
            const postId = parseInt(postItem.dataset.id);
            const post = this._posts.find(p => p.id === postId);
            if (post) {
                this.dispatchEvent(new CustomEvent("post-selected", { 
                    detail: { value: post },
                    bubbles: true,
                    composed: true
                }));
            }
        }
    }

    attributeChangedCallback(name, oldValue, newValue) {
        if (name === 'posts' && newValue) {
            try {
                this._posts = JSON.parse(newValue);
                this._render();
            } catch (e) {
                console.error('Failed to parse posts attribute:', e);
            }
        } else if (name === 'title') {
            this._render();
        }
    }
    
    _getStyles() {
        return `
            <style>
                /* Import Bootstrap CSS into shadow DOM */
                @import url("https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css");
                    @import url("/css/base.css");
                    
                    .posts-container {
                        border: 1px solid var(--border-color);
                        border-radius: 0.5rem;
                        padding: 1rem;
                        background-color: white;
                    }
                    
                    .container-title {
                        font-size: 1.25rem;
                        font-weight: 600;
                        margin-bottom: 1rem;
                        padding-bottom: 0.5rem;
                        border-bottom: 1px solid var(--border-color);
                    }
        
                    .post-item {
                        background-color: var(--light-gray);
                        border-radius: 0.75rem;
                        padding: 1rem;
                        margin-bottom: 1rem;
                        border-left: 4px solid var(--primary-purple);
                        cursor: pointer;
                        transition: all 0.2s ease;
                    }
        
                    .post-item:hover {
                        transform: translateY(-2px);
                        box-shadow: var(--box-shadow-sm);
                    }
        
                    .post-title {
                        font-weight: 600;
                        margin-bottom: 0.5rem;
                        color: var(--primary-purple);
                }
    
                .post-meta {
                    display: flex;
                    justify-content: space-between;
                    font-size: 0.85rem;
                    color: #6c757d;
                }
            </style>
        `;
    }
    
    _getPostsHtml() {
        if (!this._posts || this._posts.length === 0) {
            return '<div class="text-center text-muted py-4">No posts available</div>';
        }
    
        return this._posts.map(post => `
            <div class="post-item" data-id="${post.id}">
                <div class="post-title">${post.title}</div>
                <div class="post-meta">
                    <div class="author">${post.author}</div>
                    <div class="date">${post.date || 'Just now'}</div>
                </div>
            </div>
        `).join('');
    }
    
    _render() {
        const title = this.getAttribute('title');
        const titleHtml = title ? `<div class="container-title">${title}</div>` : '';
        
        this.shadowRoot.innerHTML = `
            ${this._getStyles()}
            <div class="posts-container">
                ${titleHtml}
                ${this._getPostsHtml()}
            </div>
        `;
    }
}

customElements.define('forum-posts', ForumPosts);