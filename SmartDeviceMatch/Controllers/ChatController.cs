using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartDeviceMatch.Data;
using SmartDeviceMatch.Models;
using SmartDeviceMatch.ViewModels;

namespace SmartDeviceMatch.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public ChatController(
            ApplicationDbContext context,
            UserManager<IdentityUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }


        // ==========================================
        // GET: Chat
        // List existing conversations
        // ==========================================

        public async Task<IActionResult> Index()
        {
            var currentUserId =
                await GetCurrentAppUserId();

            if (currentUserId == null)
            {
                return RedirectToAction(
                    "Create",
                    "Profile");
            }

            var offers =
                await _context.Offers
                    .Include(o => o.Device)
                    .Include(o => o.Buyer)
                    .Include(o => o.RepairShop)
                        .ThenInclude(r => r!.User)
                    .Where(o =>
                        o.Device != null &&
                        (
                            o.Device.OwnerId == currentUserId.Value
                            ||
                            o.BuyerId == currentUserId.Value
                            ||
                            (
                                o.RepairShop != null &&
                                o.RepairShop.UserId ==
                                    currentUserId.Value
                            )
                        ))
                    .OrderByDescending(o => o.Id)
                    .ToListAsync();


            var conversations =
                new Dictionary<string, ChatConversationViewModel>();


            foreach (var offer in offers)
            {
                if (offer.Device == null)
                {
                    continue;
                }

                int? otherUserId = null;
                string? otherUserName = null;


                // ==========================================
                // Repair conversation
                // DeviceOwner <-> RepairShop
                // ==========================================

                if (offer.OfferType == "Repair" &&
                    offer.RepairShop != null)
                {
                    var ownerId =
                        offer.Device.OwnerId;

                    var repairShopUserId =
                        offer.RepairShop.UserId;


                    if (currentUserId.Value == ownerId)
                    {
                        otherUserId =
                            repairShopUserId;

                        otherUserName =
                            offer.RepairShop.User?.FullName
                            ?? offer.RepairShop.ShopName;
                    }
                    else if (
                        currentUserId.Value ==
                        repairShopUserId)
                    {
                        otherUserId =
                            ownerId;

                        otherUserName =
                            offer.Device.Owner?.FullName
                            ?? "Device Owner";
                    }
                }


                // ==========================================
                // Purchase conversation
                // Buyer <-> RepairShop
                // ==========================================

                if (offer.OfferType == "Buy" &&
                    offer.BuyerId.HasValue &&
                    offer.RepairShop != null)
                {
                    var buyerId =
                        offer.BuyerId.Value;

                    var repairShopUserId =
                        offer.RepairShop.UserId;


                    if (currentUserId.Value == buyerId)
                    {
                        otherUserId =
                            repairShopUserId;

                        otherUserName =
                            offer.RepairShop.User?.FullName
                            ?? offer.RepairShop.ShopName;
                    }
                    else if (
                        currentUserId.Value ==
                        repairShopUserId)
                    {
                        otherUserId =
                            buyerId;

                        otherUserName =
                            offer.Buyer?.FullName
                            ?? "Buyer";
                    }
                }


                if (!otherUserId.HasValue ||
                    string.IsNullOrWhiteSpace(otherUserName))
                {
                    continue;
                }


                var key =
                    $"{offer.DeviceId}_{otherUserId.Value}";


                if (!conversations.ContainsKey(key))
                {
                    conversations[key] =
                        new ChatConversationViewModel
                        {
                            DeviceId =
                                offer.DeviceId,

                            OtherUserId =
                                otherUserId.Value,

                            OtherUserName =
                                otherUserName,

                            DeviceName =
                                $"{offer.Device.BrandName} {offer.Device.ModelName}",

                            OfferId =
                                offer.Id
                        };
                }
            }


            // ==========================================
            // Load all messages for current user
            // ==========================================

            var messages =
                await _context.ChatMessages
                    .Where(m =>
                        m.SenderId == currentUserId.Value
                        ||
                        m.ReceiverId == currentUserId.Value)
                    .OrderByDescending(m => m.SentAt)
                    .ToListAsync();


            foreach (var conversation
                     in conversations.Values)
            {
                var conversationMessages =
                    messages
                        .Where(m =>
                            m.DeviceId ==
                                conversation.DeviceId
                            &&
                            (
                                (
                                    m.SenderId ==
                                        currentUserId.Value
                                    &&
                                    m.ReceiverId ==
                                        conversation.OtherUserId
                                )
                                ||
                                (
                                    m.SenderId ==
                                        conversation.OtherUserId
                                    &&
                                    m.ReceiverId ==
                                        currentUserId.Value
                                )
                            ))
                        .ToList();


                var lastMessage =
                    conversationMessages
                        .FirstOrDefault();

                if (lastMessage != null)
                {
                    conversation.LastMessage =
                        lastMessage.Content;

                    conversation.LastMessageAt =
                        lastMessage.SentAt;
                }


                conversation.UnreadCount =
                    conversationMessages.Count(m =>
                        m.ReceiverId ==
                            currentUserId.Value
                        &&
                        !m.IsRead);
            }


            var result =
                conversations.Values
                    .OrderByDescending(c =>
                        c.LastMessageAt ?? DateTime.MinValue)
                    .ToList();

            return View(result);
        }


        // ==========================================
        // GET: Chat/Start
        // Start chat from an offer
        // ==========================================

        public async Task<IActionResult> Start(
            int offerId)
        {
            var currentUserId =
                await GetCurrentAppUserId();

            if (currentUserId == null)
            {
                return RedirectToAction(
                    "Create",
                    "Profile");
            }


            var offer =
                await _context.Offers
                    .Include(o => o.Device)
                    .Include(o => o.Buyer)
                    .Include(o => o.RepairShop)
                        .ThenInclude(r => r!.User)
                    .FirstOrDefaultAsync(o =>
                        o.Id == offerId);


            if (offer == null ||
                offer.Device == null)
            {
                return NotFound();
            }


            int? otherUserId = null;


            // ==========================================
            // Repair chat
            // ==========================================

            if (offer.OfferType == "Repair" &&
                offer.RepairShop != null)
            {
                var ownerId =
                    offer.Device.OwnerId;

                var repairShopUserId =
                    offer.RepairShop.UserId;


                if (currentUserId.Value == ownerId)
                {
                    otherUserId =
                        repairShopUserId;
                }
                else if (
                    currentUserId.Value ==
                    repairShopUserId)
                {
                    otherUserId =
                        ownerId;
                }
            }


            // ==========================================
            // Purchase chat
            // ==========================================

            if (offer.OfferType == "Buy" &&
                offer.BuyerId.HasValue &&
                offer.RepairShop != null)
            {
                var buyerId =
                    offer.BuyerId.Value;

                var repairShopUserId =
                    offer.RepairShop.UserId;


                if (currentUserId.Value == buyerId)
                {
                    otherUserId =
                        repairShopUserId;
                }
                else if (
                    currentUserId.Value ==
                    repairShopUserId)
                {
                    otherUserId =
                        buyerId;
                }
            }


            if (!otherUserId.HasValue)
            {
                return Forbid();
            }


            return RedirectToAction(
                nameof(Conversation),
                new
                {
                    deviceId =
                        offer.DeviceId,

                    otherUserId =
                        otherUserId.Value
                });
        }


        // ==========================================
        // GET: Chat/Conversation
        // ==========================================

        public async Task<IActionResult> Conversation(
            int deviceId,
            int otherUserId)
        {
            var currentUserId =
                await GetCurrentAppUserId();

            if (currentUserId == null)
            {
                return RedirectToAction(
                    "Create",
                    "Profile");
            }


            if (currentUserId.Value == otherUserId)
            {
                return BadRequest();
            }


            var allowed =
                await CanUsersChat(
                    deviceId,
                    currentUserId.Value,
                    otherUserId);

            if (!allowed)
            {
                return Forbid();
            }


            var device =
                await _context.Devices
                    .FirstOrDefaultAsync(d =>
                        d.Id == deviceId &&
                        !d.IsDeleted);


            if (device == null)
            {
                return NotFound();
            }


            var otherUser =
                await _context.AppUsers
                    .FirstOrDefaultAsync(u =>
                        u.Id == otherUserId);


            if (otherUser == null)
            {
                return NotFound();
            }


            // ==========================================
            // Mark received messages as read
            // ==========================================

            var unreadMessages =
                await _context.ChatMessages
                    .Where(m =>
                        m.DeviceId == deviceId &&
                        m.SenderId == otherUserId &&
                        m.ReceiverId == currentUserId.Value &&
                        !m.IsRead)
                    .ToListAsync();


            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
            }


            if (unreadMessages.Any())
            {
                await _context.SaveChangesAsync();
            }


            // ==========================================
            // Message history
            // ==========================================

            var messages =
                await _context.ChatMessages
                    .Where(m =>
                        m.DeviceId == deviceId
                        &&
                        (
                            (
                                m.SenderId ==
                                    currentUserId.Value
                                &&
                                m.ReceiverId ==
                                    otherUserId
                            )
                            ||
                            (
                                m.SenderId ==
                                    otherUserId
                                &&
                                m.ReceiverId ==
                                    currentUserId.Value
                            )
                        ))
                    .OrderBy(m => m.SentAt)
                    .ToListAsync();


            var viewModel =
                new ChatViewModel
                {
                    DeviceId =
                        deviceId,

                    CurrentUserId =
                        currentUserId.Value,

                    OtherUserId =
                        otherUserId,

                    DeviceName =
                        $"{device.BrandName} {device.ModelName}",

                    OtherUserName =
                        otherUser.FullName,

                    Messages =
                        messages
                            .Select(m =>
                                new ChatMessageViewModel
                                {
                                    Id = m.Id,

                                    SenderId =
                                        m.SenderId,

                                    ReceiverId =
                                        m.ReceiverId,

                                    Content =
                                        m.Content,

                                    IsRead =
                                        m.IsRead,

                                    SentAt =
                                        m.SentAt,

                                    IsMine =
                                        m.SenderId ==
                                            currentUserId.Value
                                })
                            .ToList()
                };


            return View(viewModel);
        }


        // ==========================================
        // Helper: Current AppUser
        // ==========================================

        private async Task<int?> GetCurrentAppUserId()
        {
            var identityUserId =
                _userManager.GetUserId(User);

            if (identityUserId == null)
            {
                return null;
            }

            var appUser =
                await _context.AppUsers
                    .FirstOrDefaultAsync(u =>
                        u.IdentityUserId ==
                            identityUserId);

            return appUser?.Id;
        }


        // ==========================================
        // Helper: Chat permission
        // ==========================================

        private async Task<bool> CanUsersChat(
            int deviceId,
            int firstUserId,
            int secondUserId)
        {
            var offers =
                await _context.Offers
                    .Include(o => o.Device)
                    .Include(o => o.RepairShop)
                    .Where(o =>
                        o.DeviceId == deviceId)
                    .ToListAsync();


            foreach (var offer in offers)
            {
                if (offer.Device == null)
                {
                    continue;
                }


                // DeviceOwner <-> RepairShop
                if (offer.OfferType == "Repair" &&
                    offer.RepairShop != null)
                {
                    var ownerId =
                        offer.Device.OwnerId;

                    var repairShopUserId =
                        offer.RepairShop.UserId;


                    if (
                        (firstUserId == ownerId &&
                         secondUserId == repairShopUserId)
                        ||
                        (firstUserId ==
                            repairShopUserId &&
                         secondUserId == ownerId)
                    )
                    {
                        return true;
                    }
                }


                // Buyer <-> RepairShop
                if (offer.OfferType == "Buy" &&
                    offer.BuyerId.HasValue &&
                    offer.RepairShop != null)
                {
                    var buyerId =
                        offer.BuyerId.Value;

                    var repairShopUserId =
                        offer.RepairShop.UserId;


                    if (
                        (firstUserId == buyerId &&
                         secondUserId ==
                            repairShopUserId)
                        ||
                        (firstUserId ==
                            repairShopUserId &&
                         secondUserId == buyerId)
                    )
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}