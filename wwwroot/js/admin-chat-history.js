// Admin Chat History Loader
// Tự động load lịch sử chat từ database khi admin online

(function() {
    // Đợi cho đến khi connection và conversations Map được khởi tạo
    const checkReady = setInterval(function() {
        if (typeof connection !== 'undefined' && 
            connection.state === signalR.HubConnectionState.Connected &&
            typeof conversations !== 'undefined') {
            clearInterval(checkReady);
            console.log('🔄 Bắt đầu load lịch sử chat từ database...');
            loadChatHistoryFromDatabase();
        }
    }, 500); // Check mỗi 0.5 giây

    // Timeout sau 30 giây để tránh vòng lặp vô hạn
    setTimeout(function() {
        clearInterval(checkReady);
    }, 30000);

    // Load lịch sử chat từ database khi admin vừa online
    function loadChatHistoryFromDatabase() {
        // Kiểm tra lại conversations Map
        if (typeof conversations === 'undefined') {
            console.error('❌ conversations Map chưa được khởi tạo!');
            return;
        }

        fetch('/Chat/GetConversations')
            .then(response => response.json())
            .then(data => {
                if (data.success && data.conversations && data.conversations.length > 0) {
                    console.log('📚 Đã load', data.conversations.length, 'cuộc hội thoại từ database');
                    
                    // Load từng cuộc hội thoại vào Map (mỗi connectionId chỉ load 1 lần)
                    const processedConnectionIds = new Set();
                    
                    data.conversations.forEach(conv => {
                        // Chỉ xử lý nếu connectionId chưa được xử lý
                        if (!processedConnectionIds.has(conv.connectionId)) {
                            processedConnectionIds.add(conv.connectionId);
                            
                            // Tạo conversation nếu chưa có
                            if (!conversations.has(conv.connectionId)) {
                                conversations.set(conv.connectionId, {
                                    userName: conv.userName,
                                    messages: [],
                                    unreadCount: 0
                                });
                            }

                            // Load chi tiết tin nhắn của conversation này
                            loadConversationMessages(conv.connectionId, conv.userName);
                        }
                    });
                } else {
                    console.log('📭 Không có lịch sử chat nào trong database');
                }
            })
            .catch(error => {
                console.error('❌ Lỗi load lịch sử chat:', error);
            });
    }

    // Load tin nhắn của một cuộc hội thoại cụ thể
    function loadConversationMessages(connectionId, userName) {
        // Kiểm tra lại conversations Map
        if (typeof conversations === 'undefined') {
            console.error('❌ conversations Map chưa được khởi tạo!');
            return;
        }

        fetch(`/Chat/GetChatHistory?connectionId=${connectionId}`)
            .then(response => response.json())
            .then(data => {
                if (data.success && data.messages && data.messages.length > 0) {
                    console.log(`📨 Load ${data.messages.length} tin nhắn cho ${userName}`);
                    
                    // Đảm bảo conversation đã tồn tại
                    if (!conversations.has(connectionId)) {
                        conversations.set(connectionId, {
                            userName: userName,
                            messages: [],
                            unreadCount: 0
                        });
                    }

                    const convo = conversations.get(connectionId);
                    
                    // Xóa tin nhắn cũ nếu có (tránh trùng lặp) và cập nhật userName nếu cần
                    convo.messages = [];
                    convo.userName = userName; // Cập nhật tên nếu có thay đổi
                    
                    // Thêm tất cả tin nhắn vào messages array
                    data.messages.forEach(msg => {
                        const sender = msg.isAdminMessage ? 'admin' : 'customer';
                        
                        // Check if message is image
                        if (msg.messageType === 'image' && msg.imageUrl) {
                            convo.messages.push({
                                sender: sender,
                                message: `[IMAGE]${msg.imageUrl}`,
                                time: msg.sentAt
                            });
                        } else {
                            convo.messages.push({
                                sender: sender,
                                message: msg.message,
                                time: msg.sentAt
                            });
                        }
                    });

                    // Đếm số tin nhắn chưa đọc từ khách hàng
                    const unreadCount = data.messages.filter(m => !m.isAdminMessage && !m.isRead).length;

                    // Cập nhật UI để hiển thị user trong danh sách
                    if (typeof updateUserListUI === 'function') {
                        const lastMsg = data.messages[data.messages.length - 1];
                        const lastMsgText = lastMsg.messageType === 'image' ? '📸 Đã gửi hình ảnh' : lastMsg.message;
                        
                        // Kiểm tra xem conversation đã tồn tại trong UI chưa
                        const isNew = !document.getElementById(`user-${connectionId}`);
                        updateUserListUI(connectionId, userName, lastMsgText, isNew, unreadCount);
                    } else {
                        console.warn('⚠️ updateUserListUI chưa được định nghĩa, sẽ cập nhật UI sau');
                    }
                } else {
                    console.log(`📭 Không có tin nhắn nào cho ${userName}`);
                }
            })
            .catch(error => {
                console.error(`❌ Lỗi load tin nhắn cho ${userName}:`, error);
            });
    }
})();

