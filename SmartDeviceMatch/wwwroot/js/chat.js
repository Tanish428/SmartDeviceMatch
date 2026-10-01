(function () {

    "use strict";


    const config = window.chatConfig;

    if (!config) {
        return;
    }


    const messagesContainer =
        document.getElementById("chatMessages");

    const form =
        document.getElementById("chatForm");

    const input =
        document.getElementById("messageInput");

    const typingIndicator =
        document.getElementById("typingIndicator");

    const connectionStatus =
        document.getElementById("connectionStatus");


    // ==========================================
    // SignalR connection
    // ==========================================

    const connection =
        new signalR.HubConnectionBuilder()
            .withUrl("/chatHub")
            .withAutomaticReconnect()
            .build();


    // ==========================================
    // Escape HTML
    // ==========================================

    function escapeHtml(value) {

        const div =
            document.createElement("div");

        div.textContent =
            value ?? "";

        return div.innerHTML;
    }


    // ==========================================
    // Add message to screen
    // ==========================================

    function addMessage(message) {

        const emptyChat =
            document.getElementById("emptyChat");

        if (emptyChat) {
            emptyChat.remove();
        }


        const isMine =
            message.senderId ===
            config.currentUserId;


        const wrapper =
            document.createElement("div");

        wrapper.className =
            "d-flex mb-3 " +
            (
                isMine
                    ? "justify-content-end"
                    : "justify-content-start"
            );

        wrapper.dataset.messageId =
            message.id;


        const bubble =
            document.createElement("div");

        bubble.className =
            "chat-bubble " +
            (
                isMine
                    ? "chat-mine"
                    : "chat-other"
            );


        const content =
            document.createElement("div");

        content.className =
            "chat-content";

        content.textContent =
            message.content;


        const meta =
            document.createElement("div");

        meta.className =
            "chat-meta";


        const time =
            new Date(message.sentAt)
                .toLocaleTimeString(
                    [],
                    {
                        hour: "2-digit",
                        minute: "2-digit"
                    }
                );


        meta.textContent =
            time;


        if (isMine) {

            const status =
                document.createElement("span");

            status.className =
                "message-read-status ms-1";

            status.textContent =
                message.isRead
                    ? "✓✓ Read"
                    : "✓ Sent";

            meta.appendChild(status);
        }


        bubble.appendChild(content);

        bubble.appendChild(meta);

        wrapper.appendChild(bubble);

        messagesContainer.appendChild(wrapper);


        messagesContainer.scrollTop =
            messagesContainer.scrollHeight;
    }


    // ==========================================
    // Receive message
    // ==========================================

    connection.on(
        "ReceiveMessage",
        async function (message) {

            addMessage(message);


            // If another user sent it to us,
            // immediately mark it as read.
            if (
                message.senderId !==
                config.currentUserId
            ) {

                try {

                    await connection.invoke(
                        "MarkMessagesAsRead",
                        config.deviceId,
                        config.otherUserId
                    );

                }
                catch (error) {

                    console.error(
                        "Unable to mark messages as read:",
                        error
                    );
                }
            }
        }
    );


    // ==========================================
    // Messages read
    // ==========================================

    connection.on(
        "MessagesRead",
        function (
            readerUserId,
            senderUserId
        ) {

            if (
                readerUserId !==
                config.otherUserId
            ) {
                return;
            }


            const mine =
                messagesContainer.querySelectorAll(
                    "[data-message-id]"
                );


            mine.forEach(function (element) {

                const status =
                    element.querySelector(
                        ".message-read-status"
                    );

                if (status) {
                    status.textContent =
                        "✓✓ Read";
                }

            });
        }
    );


    // ==========================================
    // Typing indicator
    // ==========================================

    let typingTimeout = null;


    connection.on(
        "UserTyping",
        function (
            userId,
            isTyping
        ) {

            if (
                userId !==
                config.otherUserId
            ) {
                return;
            }


            typingIndicator.style.display =
                isTyping
                    ? "block"
                    : "none";
        }
    );


    input.addEventListener(
        "input",
        function () {

            connection.invoke(
                "SendTyping",
                config.deviceId,
                config.otherUserId,
                true
            ).catch(function () {
                // Ignore typing errors
            });


            clearTimeout(
                typingTimeout
            );


            typingTimeout =
                setTimeout(
                    function () {

                        connection.invoke(
                            "SendTyping",
                            config.deviceId,
                            config.otherUserId,
                            false
                        ).catch(function () {
                            // Ignore typing errors
                        });

                    },
                    700
                );
        }
    );


    // ==========================================
    // Send message
    // ==========================================

    form.addEventListener(
        "submit",
        async function (event) {

            event.preventDefault();


            const content =
                input.value.trim();


            if (!content) {
                return;
            }


            if (content.length > 1000) {

                alert(
                    "Message cannot exceed 1000 characters."
                );

                return;
            }


            try {

                await connection.invoke(
                    "SendMessage",
                    config.deviceId,
                    config.otherUserId,
                    content
                );


                input.value = "";

                input.focus();


                await connection.invoke(
                    "SendTyping",
                    config.deviceId,
                    config.otherUserId,
                    false
                );

            }
            catch (error) {

                console.error(
                    "Unable to send message:",
                    error
                );

                alert(
                    "Unable to send the message."
                );
            }
        }
    );


    // ==========================================
    // Enter = Send
    // Shift + Enter = New line
    // ==========================================

    input.addEventListener(
        "keydown",
        function (event) {

            if (
                event.key === "Enter" &&
                !event.shiftKey
            ) {

                event.preventDefault();

                form.requestSubmit();
            }
        }
    );


    // ==========================================
    // Connection status
    // ==========================================

    connection.onreconnecting(
        function () {

            connectionStatus.textContent =
                "Reconnecting...";

            connectionStatus.className =
                "text-warning";
        }
    );


    connection.onreconnected(
        async function () {

            connectionStatus.textContent =
                "Connected";

            connectionStatus.className =
                "text-success";


            try {

                await connection.invoke(
                    "JoinChat",
                    config.deviceId,
                    config.otherUserId
                );

                await connection.invoke(
                    "MarkMessagesAsRead",
                    config.deviceId,
                    config.otherUserId
                );

            }
            catch (error) {

                console.error(
                    "Unable to restore chat:",
                    error
                );
            }
        }
    );


    connection.onclose(
        function () {

            connectionStatus.textContent =
                "Disconnected";

            connectionStatus.className =
                "text-danger";
        }
    );


    // ==========================================
    // Start connection
    // ==========================================

    async function start() {

        try {

            await connection.start();

            console.log(
                "Chat SignalR connected."
            );


            await connection.invoke(
                "JoinChat",
                config.deviceId,
                config.otherUserId
            );


            await connection.invoke(
                "MarkMessagesAsRead",
                config.deviceId,
                config.otherUserId
            );


            messagesContainer.scrollTop =
                messagesContainer.scrollHeight;

        }
        catch (error) {

            console.error(
                "Chat SignalR connection failed:",
                error
            );

            connectionStatus.textContent =
                "Connection failed";

            connectionStatus.className =
                "text-danger";


            setTimeout(
                start,
                5000
            );
        }
    }


    start();

})();