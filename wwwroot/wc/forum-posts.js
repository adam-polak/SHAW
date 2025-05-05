class ForumPosts extends HTMLElement {
    constructor() {
        super();
        this._posts = [];
        this.attachShadow({ mode: 'open' });
        // Bind the click handler to this instance
        this._handleClick = this._handleClick.bind(this);
    }

    static get observedAttributes() {
        return ['posts', 'selected-post'];
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
        }
    }

    _getStyles() {
        return `
            <style>
                /* Import Bootstrap CSS into shadow DOM */
                @import url("https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css");

                .post-item {
                    background-color: #f8f9fa;
                    border-radius: 0.75rem;
                    padding: 1rem;
                    margin-bottom: 1rem;
                    border-left: 4px solid #6c5ce7;
                    cursor: pointer;
                    transition: all 0.2s ease;
                }

                .post-item:hover {
                    transform: translateY(-2px);
                    box-shadow: 0 4px 8px rgba(0,0,0,0.1);
                }

                .post-title {
                    font-weight: 600;
                    margin-bottom: 0.5rem;
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
        this.shadowRoot.innerHTML = `
            ${this._getStyles()}
            <div class="posts-container">
                ${this._getPostsHtml()}
            </div>
        `;
    }
}

customElements.define('forum-posts', ForumPosts);