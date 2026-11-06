document.addEventListener('DOMContentLoaded', function () {
    var openButton = document.getElementById('chat-open-button');
    var closeButton = document.getElementById('chat-close-button');
    var widget = document.getElementById('rag-chat-widget');
    var form = document.getElementById('chat-form');
    var input = document.getElementById('chat-message-input');
    var messages = document.getElementById('chat-messages');
    var sendButton = document.getElementById('chat-send-button');
    var conversationStorageKey = 'auir-rag-chat-conversation-id';
    var conversationId = localStorage.getItem(conversationStorageKey);
    if (!conversationId) {
        conversationId = crypto.randomUUID();
        localStorage.setItem(conversationStorageKey, conversationId);
    }

    if (!openButton || !closeButton || !widget || !form || !input || !messages || !sendButton) {
        return;
    }

    function appendMessage(text, kind) {
        var message = document.createElement('p');
        message.className = 'rag-chat__message rag-chat__message--' + kind;
        message.textContent = text;
        messages.appendChild(message);
        messages.scrollTop = messages.scrollHeight;
        return message;
    }

    openButton.addEventListener('click', function () {
        widget.hidden = false;
        input.focus();
    });

    function closeWidget() {
        widget.hidden = true;
        openButton.focus();
    }

    closeButton.addEventListener('click', closeWidget);
    document.addEventListener('keydown', function (event) {
        if (event.key === 'Escape' && !widget.hidden) {
            closeWidget();
        }
    });

    form.addEventListener('submit', async function (event) {
        event.preventDefault();
        var message = input.value.trim();
        if (!message || sendButton.disabled) {
            return;
        }

        appendMessage(message, 'user');
        input.value = '';
        input.disabled = true;
        sendButton.disabled = true;
        var pendingMessage = appendMessage('Đang tìm câu trả lời...', 'assistant');

        try {
            var response = await fetch('/chat/assist', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ message: message, conversationId: conversationId })
            });
            var responseText = await response.text();
            var result;
            try {
                result = JSON.parse(responseText);
            } catch {
                if (!response.ok) {
                    throw new Error('Dịch vụ chat gặp lỗi (HTTP ' + response.status + '). Vui lòng thử lại sau.');
                }

                throw new Error('Chatbot trả về phản hồi không hợp lệ. Vui lòng thử lại.');
            }

            if (!response.ok) {
                throw new Error(result.message || result.title || 'Không thể gửi câu hỏi. Vui lòng thử lại.');
            }

            pendingMessage.textContent = result.reply || 'Chatbot chưa trả về nội dung.';
        } catch (error) {
            pendingMessage.classList.add('rag-chat__message--error');
            pendingMessage.textContent = error.message || 'Không thể kết nối tới chatbot. Vui lòng thử lại.';
        } finally {
            input.disabled = false;
            sendButton.disabled = false;
            input.focus();
            messages.scrollTop = messages.scrollHeight;
        }
    });
});
