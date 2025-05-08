/**
 * ForumComments Web Component
 * 
 * @description Displays a list of comments on a forum post
 * 
 * @property {number} postId - The associated post id
 * @property {Array} comments - Comments on a post
 * 
 * Expected JSON Schema for comments:
 * [
 *   {
 *     "id": number,          // Unique identifier for the comment
 *     "text": string,       // Comment to display,
 *     "date": string        // Date the comment was posted
 *     "username": string,      // Username of comment author
 *   },
 *   ...
 * ]
 * 
 * @fires post-selected - Custom event when user clicks on a post
 * @property {Object} event.detail.value - The selected post object
 */
class PostComments extends HTMLElement {
    constructor() {
        super();
        this._comments = [];
        this.attachShadow({ mode: 'open' });
    }

    static get observedAttributes() {
        return ['comments'];
    }

    connectedCallback() {
        console.log('PostComments connected');
        this._render();
    }

    attributeChangedCallback(name, _, newValue) {
        if (name === 'comments' && newValue) {
            try {
                console.log('Comments attribute changed:', newValue);
                this._comments = JSON.parse(newValue);
                console.log('Parsed comments:', this._comments);
                this._render();
            } catch (e) {
                console.error('Failed to parse comments attribute:', e, 'Raw value:', newValue);
                this._render();
            }
        }
    }
    
    _getStyles() {
        return `
            <style>
                /* Import Bootstrap CSS into shadow DOM */
                @import url("https://cdn.jsdelivr.net/npm/bootstrap@5.3.2/dist/css/bootstrap.min.css");
                @import url("/css/base.css");

                .comments-container {
                    border: 1px solid var(--border-color);
                    border-radius: 0.5rem;
                    padding: .5rem .3rem;
                    background-color: #f9f9f9;
                    overflow-y: scroll;
                    height: 200px;
                }

                .comment-item {
                    background-color: #ffffff;
                    border-radius: 0.5rem;
                    padding: 0.75rem 1rem;
                    border: 1px solid var(--border-color);
                    transition: background-color 0.2s ease;
                }

                .comment-author {
                    font-size: 1.3rem;
                    font-weight: 600;
                    color: var(--primary-purple);
                }

                .comment-text {
                    font-size: 1.3rem;
                    color: #333;
                }

                .comment-meta {
                    font-size: 0.7rem;
                    color: #6c757d;
                    display: flex;
                    justify-content: space-between;
                }
            </style>
        `;
    }
    
    _getCommentsHtml() {
        if (!this._comments || this._comments.length === 0) {
            return '<div class="text-center text-muted py-4">No commments yet</div>';
        }
    
        return this._comments.map(comment => `
            <div class="comment-item mb-2" data-id="${comment.id}">
                <div class="comment-meta mb-2">
                    <div class="date">${comment.date || 'Just now'}</div>
                </div>
                <div class="d-flex gap-1 mb-2">
                    <div class="comment-author">${comment.username}:</div>
                    <div class="comment-text">${comment.text}</div>
                </div>
            </div>
        `).join('');
    }
    
    _render() {
        this.shadowRoot.innerHTML = `
            ${this._getStyles()}
            <hr></hr>
            <div class="comments-container">
                ${this._getCommentsHtml()}
            </div>
        `;
    }
}

customElements.define('post-comments', PostComments);