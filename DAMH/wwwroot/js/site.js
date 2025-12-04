document.addEventListener('DOMContentLoaded', function() {
    const bookImages = document.querySelectorAll('img[src*="CoverImageUrl"], .card-img-top, .book-cover');

    bookImages.forEach(img => {
        img.addEventListener('error', function() {
            this.onerror = null;
            this.src = 'https://placehold.co/400x600/e9ecef/6c757d?text=No+Image';
            this.classList.add('book-cover-placeholder');
        });
    });

    if (document.querySelector('.favorite-count-badge')) {
        updateFavoriteCount();
    }
});

async function toggleFavorite(bookId, button) {
    try {
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        const response = await fetch('/Favorites/Toggle', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
                ...(token ? { 'RequestVerificationToken': token } : {})
            },
            body: `bookId=${bookId}`
        });

        const result = await response.json();
        if (result.success) {
            const buttons = document.querySelectorAll(`.favorite-btn[data-book-id="${bookId}"]`);
            buttons.forEach(btn => {
                const icon = btn.querySelector('i');
                if (result.isFavorited) {
                    icon.classList.remove('bi-heart');
                    icon.classList.add('bi-heart-fill', 'text-danger');
                    btn.classList.add('is-favorited');
                } else {
                    icon.classList.remove('bi-heart-fill', 'text-danger');
                    icon.classList.add('bi-heart');
                    btn.classList.remove('is-favorited');
                }
            });
            if (result.isFavorited) {
                showToast('Đã thêm vào yêu thích!', 'success');
            } else {
                showToast('Đã xóa khỏi yêu thích!', 'info');
            }
            updateFavoriteCount();
        } else {
            if (result.message?.includes('đăng nhập')) {
                window.location.href = '/Account/Login';
            } else {
                showToast(result.message, 'error');
            }
        }
    } catch (error) {
        console.error('Error toggling favorite:', error);
        showToast('Có lỗi xảy ra. Vui lòng thử lại!', 'error');
    }
}

async function checkFavoriteStatus(bookId) {
    try {
        const response = await fetch(`/Favorites/CheckStatus?bookId=${bookId}`);
        const result = await response.json();
        if (result.isFavorited) {
            const buttons = document.querySelectorAll(`.favorite-btn[data-book-id="${bookId}"]`);
            buttons.forEach(btn => {
                const icon = btn.querySelector('i');
                icon.classList.remove('bi-heart');
                icon.classList.add('bi-heart-fill', 'text-danger');
                btn.classList.add('is-favorited');
            });
        }
    } catch (error) {
        console.error('Error checking favorite status:', error);
    }
}

function showToast(message, type = 'info') {
    const existingToast = document.querySelector('.custom-toast');
    if (existingToast) existingToast.remove();

    const toast = document.createElement('div');
    toast.className = `custom-toast alert alert-${type === 'success' ? 'success' : type === 'error' ? 'danger' : 'info'} alert-dismissible fade show`;
    toast.style.cssText = 'position: fixed; top: 80px; right: 20px; z-index: 9999; min-width: 300px; box-shadow: 0 5px 15px rgba(0,0,0,0.3);';
    toast.innerHTML = `
        <i class="bi bi-${type === 'success' ? 'check-circle-fill' : type === 'error' ? 'exclamation-triangle-fill' : 'info-circle-fill'}"></i>
        ${message}
        <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
    `;
    document.body.appendChild(toast);

    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}

function updateFavoriteCount() {
    fetch('/Favorites/GetCount')
        .then(response => response.json())
        .then(data => {
            const countBadge = document.querySelector('.favorite-count-badge');
            if (countBadge && data.count !== undefined) {
                countBadge.textContent = data.count;
            }
        })
        .catch(error => console.error('Error updating count:', error));
}

const detailWrapper = document.querySelector('.book-detail-page');
if (detailWrapper) {
    const initDetailFavorite = () => {
        const bookId = detailWrapper.dataset.bookId;
        if (bookId) checkFavoriteStatus(bookId);
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initDetailFavorite);
    } else {
        initDetailFavorite();
    }
}


document.addEventListener('DOMContentLoaded', function () {
    const images = document.querySelectorAll('img');
    images.forEach(img => {
        if (!img.hasAttribute('onerror')) {
            img.onerror = function () {
                this.onerror = null;
                this.src = 'https://placehold.co/400x600/e9ecef/6c757d?text=No+Image';
                this.classList.add('book-cover-placeholder');
            };
        }
    });

    const observer = new MutationObserver(function (mutations) {
        mutations.forEach(function (mutation) {
            mutation.addedNodes.forEach(function (node) {
                if (node.nodeName === 'IMG' && !node.hasAttribute('onerror')) {
                    node.onerror = function () {
                        this.onerror = null;
                        this.src = 'https://placehold.co/400x600/e9ecef/6c757d?text=No+Image';
                        this.classList.add('book-cover-placeholder');
                    };
                }
            });
        });
    });

    observer.observe(document.body, {
        childList: true,
        subtree: true
    });
});

function toggleFavorite(bookId, button) {
    if (!button) return;

    $.post('/Favorites/Toggle', { bookId: bookId }, function (response) {
        if (response.success) {
            var btn = $(button);
            var icon = btn.find('i');

            if (response.isFavorited) {
                icon.removeClass('bi-heart').addClass('bi-heart-fill text-danger');
                btn.removeClass('btn-outline-danger').addClass('btn-danger');
            } else {
                icon.removeClass('bi-heart-fill text-danger').addClass('bi-heart');
                btn.removeClass('btn-danger').addClass('btn-outline-danger');
            }

            showNotification(response.message, 'success');
        } else {
            showNotification(response.message || 'Error occurred', 'error');
        }
    }).fail(function () {
        showNotification('Please login to add favorites', 'error');
    });
}

function showNotification(message, type) {
    var alertClass = type === 'success' ? 'alert-success' : 'alert-danger';
    var notification = $('<div class="alert ' + alertClass + ' alert-dismissible fade show position-fixed" style="top: 80px; right: 20px; z-index: 9999; min-width: 300px;" role="alert">' +
        message +
        '<button type="button" class="btn-close" data-bs-dismiss="alert"></button>' +
        '</div>');

    $('body').append(notification);
    setTimeout(function () {
        notification.alert('close');
    }, 3000);
}

$(document).ready(function () {
    $('.favorite-btn').each(function () {
        var bookId = $(this).data('book-id');
        var btn = $(this);

        if (bookId) {
            $.get('/Favorites/CheckStatus', { bookId: bookId }, function (response) {
                if (response.isFavorited) {
                    btn.find('i').removeClass('bi-heart').addClass('bi-heart-fill text-danger');
                    btn.removeClass('btn-outline-danger').addClass('btn-danger');
                }
            });
        }
    });
});