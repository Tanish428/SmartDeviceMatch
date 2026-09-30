(function () {

    "use strict";

    const badge =
        document.getElementById("notificationBadge");

    const notificationList =
        document.getElementById("notificationList");


    // ==========================================
    // Notification Icon Count
    // ==========================================

    function updateBadge(count) {

        if (!badge) {
            return;
        }

        if (count > 0) {

            badge.textContent =
                count > 99 ? "99+" : count;

            badge.classList.remove("d-none");

        } else {

            badge.textContent = "0";

            badge.classList.add("d-none");
        }
    }


    // ==========================================
    // Notification Icon
    // ==========================================

    function getNotificationIcon(type) {

        switch (type) {

            case "NewOffer":
                return "🔔";

            case "OfferAccepted":
                return "✅";

            case "OfferRejected":
                return "❌";

            case "EscrowAlert":
                return "💰";

            case "DeviceVerified":
                return "📱";

            case "PaymentReleased":
                return "💵";

            case "MatchAlert":
                return "🎯";

            default:
                return "🔔";
        }
    }


    // ==========================================
    // Format Date
    // ==========================================

    function formatDate(dateString) {

        const date =
            new Date(dateString);

        return date.toLocaleString(
            undefined,
            {
                day: "2-digit",
                month: "short",
                hour: "2-digit",
                minute: "2-digit"
            }
        );
    }


    // ==========================================
    // Render Notification
    // ==========================================

    function renderNotification(notification) {

        const wrapper =
            document.createElement("div");

        wrapper.className =
            "notification-dropdown-item border-bottom p-3";

        if (!notification.isRead) {
            wrapper.classList.add("bg-light");
        }

        wrapper.dataset.notificationId =
            notification.id;


        wrapper.innerHTML = `
            <div class="d-flex">

                <div class="me-2"
                     style="font-size: 22px;">
                    ${getNotificationIcon(notification.type)}
                </div>

                <div class="flex-grow-1">

                    <div class="d-flex justify-content-between">

                        <strong>
                            ${escapeHtml(notification.title)}
                        </strong>

                        ${
                            !notification.isRead
                                ? '<span class="badge bg-primary ms-2">New</span>'
                                : ''
                        }

                    </div>

                    <div class="small text-muted mt-1">
                        ${escapeHtml(notification.message)}
                    </div>

                    <div class="small text-secondary mt-1">
                        ${formatDate(notification.createdAt)}
                    </div>

                </div>

            </div>
        `;

        return wrapper;
    }


    // ==========================================
    // Escape HTML
    // ==========================================

    function escapeHtml(value) {

        if (value == null) {
            return "";
        }

        const div =
            document.createElement("div");

        div.textContent = value;

        return div.innerHTML;
    }


    // ==========================================
    // Load Recent Notifications
    // ==========================================

    async function loadRecentNotifications() {

        if (!notificationList) {
            return;
        }

        try {

            const response =
                await fetch(
                    "/Notification/Recent");

            if (!response.ok) {
                return;
            }

            const notifications =
                await response.json();

            notificationList.innerHTML = "";

            if (notifications.length === 0) {

                notificationList.innerHTML = `
                    <div class="text-center text-muted p-4">
                        No notifications yet.
                    </div>
                `;

                return;
            }

            notifications.forEach(notification => {

                notificationList.appendChild(
                    renderNotification(notification)
                );

            });

        }
        catch (error) {

            console.error(
                "Unable to load notifications:",
                error
            );
        }
    }


    // ==========================================
    // Load Unread Count
    // ==========================================

    async function loadUnreadCount() {

        try {

            const response =
                await fetch(
                    "/Notification/UnreadCount");

            if (!response.ok) {
                return;
            }

            const result =
                await response.json();

            updateBadge(result.count);

        }
        catch (error) {

            console.error(
                "Unable to load notification count:",
                error
            );
        }
    }


    // ==========================================
    // SignalR Connection
    // ==========================================

    const connection =
        new signalR.HubConnectionBuilder()
            .withUrl("/notificationHub")
            .withAutomaticReconnect()
            .build();


    // ==========================================
    // Receive Real-Time Notification
    // ==========================================

    connection.on(
        "ReceiveNotification",
        function (notification) {

            console.log(
                "New notification:",
                notification
            );


            // Increase unread badge
            const currentCount =
                badge &&
                !badge.classList.contains("d-none")
                    ? parseInt(badge.textContent) || 0
                    : 0;

            updateBadge(
                currentCount + 1
            );


            // Add notification to dropdown
            if (notificationList) {

                const emptyMessage =
                    notificationList.querySelector(
                        ".text-center"
                    );

                if (emptyMessage) {
                    notificationList.innerHTML = "";
                }

                notificationList.prepend(
                    renderNotification(notification)
                );
            }


            // Optional browser notification
            showBrowserNotification(notification);
        }
    );


    // ==========================================
    // Start SignalR
    // ==========================================

    async function startSignalR() {

        try {

            await connection.start();

            console.log(
                "SignalR connected."
            );

        }
        catch (error) {

            console.error(
                "SignalR connection failed:",
                error
            );

            setTimeout(
                startSignalR,
                5000
            );
        }
    }


    connection.onreconnecting(
        function () {

            console.log(
                "SignalR reconnecting..."
            );

        }
    );


    connection.onreconnected(
        function () {

            console.log(
                "SignalR reconnected."
            );

            loadUnreadCount();
            loadRecentNotifications();

        }
    );


    connection.onclose(
        function () {

            console.log(
                "SignalR connection closed."
            );

        }
    );


    // ==========================================
    // Browser Notification
    // ==========================================

    function showBrowserNotification(notification) {

        if (!("Notification" in window)) {
            return;
        }

        if (Notification.permission === "granted") {

            new Notification(
                notification.title,
                {
                    body: notification.message,
                    icon: "/favicon.ico"
                }
            );
        }
    }


    // ==========================================
    // Ask Browser Notification Permission
    // ==========================================

    function requestBrowserNotificationPermission() {

        if (!("Notification" in window)) {
            return;
        }

        if (Notification.permission === "default") {

            Notification.requestPermission()
                .catch(() => {
                    // Ignore permission errors
                });
        }
    }


    // ==========================================
    // Initialization
    // ==========================================

    document.addEventListener(
        "DOMContentLoaded",
        function () {

            loadUnreadCount();

            loadRecentNotifications();

            startSignalR();

            requestBrowserNotificationPermission();
        }
    );

})();