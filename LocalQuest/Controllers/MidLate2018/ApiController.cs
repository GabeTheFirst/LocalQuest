using HttpMultipartParser;
using LocalQuest.Models.Mid2018;
using LocalQuest.Models.MidLate2018;
using QuerryNetworking.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace LocalQuest.Controllers.MidLate2018
{
    internal class ApiController : ClientRequest
    {
        [Get("/")]
        public Nameserver GetNameserver()
        {
            string? PortOverride = Config.GetString("ServerPort");
            if (string.IsNullOrEmpty(PortOverride))
            {
                PortOverride = "16512";
            }

            return new Nameserver()
            {
                API = "http://localhost:" + PortOverride + "/",
                Auth = "http://localhost:" + PortOverride + "/",
                Images = "http://localhost:" + PortOverride + "/img/",
                Notifications = "ws://localhost:" + PortOverride + "/",
                WWW = "http://localhost:" + PortOverride + "/"
            };
        }

        [Get("/api/config/v2")]
        public Models.Mid2018.Config GetConfig()
        {
            return new Models.Mid2018.Config();
        }

        [Post("/api/objectives/v1/updateobjective")]
        public void UpdateObjective()
        {
            // update objectives here soon
        }

        [Get("/api/equipment/v1/getUnlocked")]
        public List<EquipmentItem> GetEquipment()
        {
            return new List<EquipmentItem>();
        }

        [Get("/api/playerevents/v1/all")]
        public PlayerEventData GetAllEvents()
        {
            return new PlayerEventData();
        }

        [Get("/api/challenge/v1/getCurrent")]
        public SuccessResponse GetCurrentChallenge()
        {
            return new SuccessResponse()
            {
                Success = true,
                Message = JsonSerializer.Serialize(new WeeklyChallenges()
                {
                    Challenges = new List<WeeklyChallenge>()
                    {
                        new WeeklyChallenge()
                        {
                            ChallengeId = new Random().Next(),
                            Complete = false,
                            Config = JsonSerializer.Serialize(new BaseChallengeConfig()
                            {
                                c = false,
                                ct = ChallengeTypes.DiscGolfFinishUnderParChallenge
                            }),
                            Description = "complete disc golf under par"
                        },
                        new WeeklyChallenge()
                        {
                            ChallengeId = new Random().Next(),
                            Complete = false,
                            Config = JsonSerializer.Serialize(new BaseChallengeConfig()
                            {
                                c = false,
                                ct = ChallengeTypes.DiscGolfFinishUnderParChallenge
                            }),
                            Description = "enter the jerry juke room"
                        },
                        new WeeklyChallenge()
                        {
                            ChallengeId = new Random().Next(),
                            Complete = false,
                            Config = JsonSerializer.Serialize(new BaseChallengeConfig()
                            {
                                c = false,
                                ct = ChallengeTypes.DiscGolfFinishUnderParChallenge
                            }),
                            Description = "green zone"
                        }
                    },
                    Gifts = new List<WeeklyGift>()
                    { 
                        new WeeklyGift()
                        {
                            AvatarItemDesc = "",
                            ConsumableItemDesc = "",
                            StorefrontType = 0,
                            Xp = 0,
                            Level = 1,
                            EquipmentModificationGuid = "a6c869d9-8f30-4ce6-a7ea-cb7ea9f9d492",
                            EquipmentPrefabName = "[PaintballShield]",
                            GiftDropId = 1,
                            GiftContext = GiftContext.All_Weekly_Challenge_Complete
                        }
                    }
                })
            };
        }

        [Post("/api/gamesessions/v3/joinroom")]
        public async Task<Models.MidLate2018.MatchmakeResponse> JoinCustomRoom()
        {
            string PostData = GetPostString();
            Log.Debug(PostData);
            Models.MidLate2018.JoinRoomRequest? Request = JsonSerializer.Deserialize<Models.MidLate2018.JoinRoomRequest>(PostData);
            if (Request == null)
            {
                return new Models.MidLate2018.MatchmakeResponse()
                {
                    Result = Models.MidLate2018.MatchmakeResult.NoSuchGame
                };
            }

            if (RoomManager.AllRooms == null)
            {
                Log.Warn("Rooms not found!");
                return new Models.MidLate2018.MatchmakeResponse()
                {
                    Result = Models.MidLate2018.MatchmakeResult.NoSuchRoom
                };
            }
            RoomBase? Room = RoomManager.AllRooms.FirstOrDefault(A => A.Name != null && Request.RoomName != null && A.Name.ToLower() == Request.RoomName.ToLower());
            if(Room == null)
            {
                return new Models.MidLate2018.MatchmakeResponse()
                {
                    Result = Models.MidLate2018.MatchmakeResult.NoSuchRoom
                };
            }

            RoomManager.JoinTime(Room.RoomId);

            Models.MidLate2018.GameSession NewSession = new Models.MidLate2018.GameSession()
            {
                RoomSceneLocationId = Room.Scenes[0].UnitySceneId,
                RoomId = Room.RoomId,
                PhotonRoomId = Room.Scenes[0].UnitySceneId + Room.RoomId,
                GameSessionId = 1 + Room.RoomId,
                IsSandbox = Room.Sandbox,
                RoomSceneId = Room.Scenes[0].SceneId,
                DataBlobName = Room.Scenes[0].DataBlob,
                EventId = null,
                GameInProgress = false,
                IsFull = false,
                MaxCapacity = 12,
                Name = "^" + Room.Name,
                Private = Request.Private,
                PhotonRegionId = "us"
            };

            if (Request.Private && !string.IsNullOrEmpty(Config.GetString("PrivateCode")))
            {
                NewSession.PhotonRoomId += Config.GetString("PrivateCode");
            }

            await Notify.SendNotification(new Notification()
            {
                Id = Models.MidLate2018.NotificationType.SubscriptionUpdatePresence,
                Msg = new Presence()
                {
                    GameSession = NewSession,
                    PlayerType = PlayerType.SCREEN,
                    IsOnline = true,
                    PlayerId = long.Parse(LocalQuest.Config.GetString("AccountId"))
                }
            });
            Notify.CurrentPresence = new Presence()
            {
                GameSession = NewSession,
                PlayerType = PlayerType.SCREEN,
                IsOnline = true,
                PlayerId = long.Parse(LocalQuest.Config.GetString("AccountId"))
            };
            Log.Debug(JsonSerializer.Serialize(new Models.MidLate2018.MatchmakeResponse()
            {
                Result = Models.MidLate2018.MatchmakeResult.Success,
                GameSession = NewSession,
                RoomDetails = new RoomDetails(Room)
            }));
            return new Models.MidLate2018.MatchmakeResponse()
            {
                Result = Models.MidLate2018.MatchmakeResult.Success,
                GameSession = NewSession,
                RoomDetails = new RoomDetails(Room)
            };
        }

        [Get("/api/rooms/v1/featuredRoomGroup")]
        public FeaturedRoomGroup GetFeaturedRooms()
        {
            List<FeaturedRoom> Rooms = new List<FeaturedRoom>();
            if(RoomManager.AllRooms == null)
            {
                Log.Warn("rooms not found.");
                return new FeaturedRoomGroup()
                {
                    Name = "no rooms"
                };
            }
            foreach (var Room in RoomManager.AllRooms.OrderByDescending(A => A.CreationTime).Take(10))
            {
                if(string.IsNullOrEmpty(Room.ImageName))
                {
                    Room.ImageName = "DefaultPFP";
                }
                if (string.IsNullOrEmpty(Room.Name))
                {
                    Room.Name = "Invalid Room";
                }
                Rooms.Add(new FeaturedRoom()
                {
                    RoomName = Room.Name,
                    ImageName = Room.ImageName,
                    RoomId = Room.RoomId
                });
            }
            return new FeaturedRoomGroup()
            {
                FeaturedRooms = Rooms
            };
        }

        [Get("/api/activities/charades/v1/words")]
        public List<CharadesWord> GetCharadesWords()
        {
            if (Charades.CharadesWords != null)
                return Charades.CharadesWords;
            return new List<CharadesWord>();
        }

        [Get("/api/objectives/v1/myprogress")]
        public ObjectiveProgress GetProgress()
        {
            return new ObjectiveProgress();
        }

        [Get("/api/checklist/v1/current")]
        public List<ChecklistItem> Checklist()
        {
            return new List<ChecklistItem>();
        }

        [Get("/api/avatar/v2/gifts")]
        public List<GiftPackage> GetGifts()
        {
            return new List<GiftPackage>();
        }

        [Get("/api/consumables/v1/getUnlocked")]
        public List<Consumable> GetConsumables()
        {
            return new List<Consumable>();
        }

        [Get("/api/avatar/v2/saved")]
        public List<SavedOutfit> GetSavedOutfits()
        {
            return AvatarManager.GetSavedOutfits();
        }

        [Get("/api/avatar/v1/saved")]
        public List<SavedOutfitV1> GetSavedOutfitsV1()
        {
            return AvatarManager.GetSavedOutfits().Select(A => new SavedOutfitV1(A)).ToList();
        }

        [Post("/api/avatar/v2/saved/set")]
        public SavedOutfit? SetSavedOutfit()
        {
            string PostString = GetPostString();
            SavedOutfitReq? Request = JsonSerializer.Deserialize<SavedOutfitReq>(PostString);
            if (Request == null)
            {
                Log.Warn("Invalid saved outfit");
                return null;
            }
            return AvatarManager.SaveOutfit(Request);
        }

        [Get("/api/avatar/v3/saved")]
        public List<SavedOutfit> GetSavedOutfitsV3()
        {
            return new List<SavedOutfit>();
        }

        [Post("/api/presence/v2/setscreenmode")]
        public void SetScreen()
        {
            // don't need to return anything for this, supposed to set screenmode on your presense
            // but this is localhost
        }

        [Post("/api/presence/v1/setplayertype")]
        public void SetPlayerType()
        {
            // don't need to return anything for this, supposed to set screenmode on your presense
            // but this is localhost
        }

        [Post("/api/platformlogin/v1/getcachedlogins")]
        public List<Profile> GetLogins()
        {
            return new List<Profile>()
            {
                new Profile()
            };
        }

        [Post("/api/platformlogin/v6")]
        public LoginV6 LoginV6()
        {
            return new LoginV6()
            {
                PlayerId = long.Parse(Config.GetString("AccountId"))
            };
        }

        [Post("/api/platformlogin/v1/profiles")]
        public List<Profile> GetProfiles()
        {
            return new List<Profile>()
            {
                new Profile()
            };
        }

        [Get("/api/PlayerReporting/v1/moderationBlockDetails")]
        public BlockDetails GetBan()
        {
            return new BlockDetails();
        }

        [Get("/img/alt/{var}")]
        public byte[] GetImg(string ImageName)
        {
            ImageName = ImageName.Split("?")[0];
            if(!ImageName.EndsWith(".png"))
            {   
                ImageName = ImageName + ".png";
            }
            return FileManager.GetBytes("Images/" + ImageName);
        }

        [Get("//img/alt/{var}")]
        public byte[] GetImgDouble(string ImageName)
        {
            ImageName = ImageName.Split("?")[0];
            if (!ImageName.EndsWith(".png"))
            {
                ImageName = ImageName + ".png";
            }
            return FileManager.GetBytes("Images/" + ImageName);
        }

        [Get("/img/{var}")]
        public byte[] GetImg2(string ImageName)
        {
            if (!ImageName.EndsWith(".png"))
            {
                ImageName = ImageName + ".png";
            }
            byte[] Image = FileManager.GetBytes("Images/" + ImageName);
            if(Image.Length == 0)
            {
                Image = NetworkFiles.GetData("Images/" + ImageName).Result;
                if(Image.Length > 0)
                {
                    FileManager.WriteBytes("Images/" + ImageName, Image);
                }
                else
                {
                    Context.Response.StatusCode = 404;
                    return new byte[0];
                }
            }
            return Image;
        }

        [Get("//img/{var}")]
        public byte[] GetImg2Double(string ImageName)
        {
            if (!ImageName.EndsWith(".png"))
            {
                ImageName = ImageName + ".png";
            }
            byte[] Image = FileManager.GetBytes("Images/" + ImageName);
            if (Image.Length == 0)
            {
                Image = NetworkFiles.GetData("Images/" + ImageName).Result;
                if (Image.Length > 0)
                {
                    FileManager.WriteBytes("Images/" + ImageName, Image);
                }
                else
                {
                    Context.Response.StatusCode = 404;
                    return new byte[0];
                }
            }
            return Image;
        }

        [Get("//room/{var}")]
        public byte[] GetRoomData(string RoomName)
        {
            if (!RoomName.EndsWith(".room"))
            {
                RoomName = RoomName + ".room";
            }
            byte[] RoomData = FileManager.GetBytes("RoomData/" + RoomName);
            if (RoomData.Length == 0)
            {
                RoomData = NetworkFiles.GetData("RoomFiles/" + RoomName).Result;
                if (RoomData.Length > 0)
                {
                    FileManager.WriteBytes("RoomData/" + RoomName, RoomData);
                }
                else
                {
                    Context.Response.StatusCode = 404;
                    return new byte[0];
                }
            }
            return RoomData;
        }

        [Get("/api/images/v2/named")]
        public List<NamedImage> GetNamedImages()
        {
            return new List<NamedImage>();
        }

        [Get("/api/config/v1/amplitude")]
        public AmplitudeConfig GetAmplitude()
        {
            return new AmplitudeConfig();
        }

        [Post("/api/platformlogin/v1/logincached")]
        public LoginResponse Login()
        {
            return new LoginResponse();
        }

        [Get("/api/messages/v2/get")]
        public List<Message> GetMessages()
        {
            return new List<Message>();
        }

        [Get("/api/relationships/v2/get")]
        public List<Relationship> Relationships()
        {
            return new List<Relationship>();
        }

        [Get("/api/settings/v2/")]
        public List<PlayerSetting>? GetSettings()
        {
            return FileManager.GetJSON<List<PlayerSetting>>("PlayerData/Settings");
        }

        [Get("/api/gameconfigs/v1/all")]
        public List<GameConfig> GetAllGameConfigs()
        {
            return new List<GameConfig>()
            {
                new GameConfig()
                {
                    Key = "UseHeartbeatWebSocket",
                    Value = "0"
                },
                new GameConfig()
                {
                    Key = "Door.Creative.Title",
                    Value = "PUZZLE"
                },
                new GameConfig()
                {
                    Key = "Door.Creative.Query",
                    Value = "#puzzle"
                },
                new GameConfig()
                {
                    Key = "Door.Featured.Title",
                    Value = "featured"
                },
                new GameConfig()
                {
                    Key = "Door.Featured.Query",
                    Value = "#featured"
                },
                new GameConfig()
                {
                    Key = "Door.Quests.Title",
                    Value = "quests"
                },
                new GameConfig()
                {
                    Key = "Door.Quests.Query",
                    Value = "#rro #quest"
                },
                new GameConfig()
                {
                    Key = "Door.Shooters.Title",
                    Value = "PVP"
                },
                new GameConfig()
                {
                    Key = "Door.Shooters.Query",
                    Value = "#rro #pvp"
                },
                new GameConfig()
                {
                    Key = "Door.Sports.Title",
                    Value = "Sports and Rec"
                },
                new GameConfig()
                {
                    Key = "Door.Sports.Query",
                    Value = "#rro #sport"
                },
                new GameConfig()
                {
                    Key = "forceRegistration",
                    Value = "false"
                },
                new GameConfig()
                {
                    Key = "Gift.MaxDaily",
                    Value = "10"
                },
                new GameConfig()
                {
                    Key = "Gift.DropChance",
                    Value = "1"
                }
            };
        }

        [Get("/api/avatar/v3/items")]
        public List<AvatarItem> GetAvatarItems()
        {
            if(AvatarManager.AvatarItems == null)
            {
                Log.Warn("Invalid avatar items list?!");
                return new List<AvatarItem>();
            }
            return AvatarManager.AvatarItems;
        }

        [Get("/api/versioncheck/v3")]
        public VersionResponse CheckVersion()
        {
            return new VersionResponse();
        }

        [Get("/api/rooms/v2/instancedetails/{var}")]
        public InstanceDetails RoomInstanceDetails(string RoomId)
        {
            return new InstanceDetails()
            {
                GameSessionCount = 0,
                PlayerCount = 0
            };
        }

        [Get("/api/rooms/v2/personaldetails/{var}")]
        public RoomPersonalDetails PersonalDetails(string RoomId)
        {
            List<long> Bookmarks = RoomManager.GetBookmarks();
            return new RoomPersonalDetails()
            {
                IsBookmarked = Bookmarks.Contains(long.Parse(RoomId)),
                IsCheering = false
            };
        }

        [Get("/api/rooms/v3/details/{var}")]
        public FullRoom? GetRoomDetails(string RoomId)
        {
            if (RoomManager.AllRooms == null)
            {
                Context.Response.StatusCode = 404;
                Log.Warn("no rooms found!");
                return null;
            }
            RoomBase? Room = RoomManager.AllRooms.FirstOrDefault(A => A.RoomId == long.Parse(RoomId));
            if (Room == null)
            {
                Context.Response.StatusCode = 404;
                Log.Warn("room not found!");
                return null;
            }
            return new FullRoom(Room);
        }

        [Get("/api/rooms/v4/details/{var}")]
        public RoomDetails? GetRoomDetailsV4(string RoomId)
        {
            if (RoomManager.AllRooms == null)
            {
                Context.Response.StatusCode = 404;
                Log.Warn("no rooms found!");
                return null;
            }
            RoomBase? Room = RoomManager.AllRooms.FirstOrDefault(A => A.RoomId == long.Parse(RoomId));
            if (Room == null)
            {
                Context.Response.StatusCode = 404;
                Log.Warn("room not found!");
                return null;
            }
            return new RoomDetails(Room);
        }

        [Get("/api/rooms/v2/myrooms")]
        public List<Models.MidLate2018.Room>? GetMyRooms()
        {
            DateTime SupportedDate = DateTime.MinValue;

            if (string.IsNullOrEmpty(StartManager.GameVersion))
            {
                Log.Warn("Unknown game version for room filter!");
                return new List<Models.MidLate2018.Room>();
            }

            string Year = StartManager.GameVersion.Substring(0, 4);
            string Month = StartManager.GameVersion.Substring(4, 2);
            string Day = StartManager.GameVersion.Substring(6, 2);
            Log.Debug($"{Year} {Month} {Day}");

            SupportedDate = SupportedDate.AddYears(int.Parse(Year) - 1);
            SupportedDate = SupportedDate.AddMonths(int.Parse(Month) - 1);
            SupportedDate = SupportedDate.AddDays(int.Parse(Day) + 5);

            Log.Debug($"{SupportedDate.Year} {SupportedDate.Month} {SupportedDate.Day}");

            List<RoomBase> Mine = RoomManager.AllRooms.Where(A => A.MinSupportedDate <= SupportedDate && A.CreatorPlayerId == long.Parse(Config.GetString("AccountId"))).ToList();
            List<Models.MidLate2018.Room> Results = new List<Models.MidLate2018.Room>();
            foreach (var Room in Mine)
            {
                Results.Add(new RoomDetails(Room).Room);
            }
            return Results;
        }

        [Get("/api/rooms/v2/baserooms")]
        public List<Models.MidLate2018.Room>? GetBaseRooms()
        {
            DateTime SupportedDate = DateTime.MinValue;

            if (string.IsNullOrEmpty(StartManager.GameVersion))
            {
                Log.Warn("Unknown game version for room filter!");
                return new List<Models.MidLate2018.Room>();
            }

            string Year = StartManager.GameVersion.Substring(0, 4);
            string Month = StartManager.GameVersion.Substring(4, 2);
            string Day = StartManager.GameVersion.Substring(6, 2);

            Log.Debug($"{Year} {Month} {Day}");

            SupportedDate = SupportedDate.AddYears(int.Parse(Year) - 1);
            SupportedDate = SupportedDate.AddMonths(int.Parse(Month) - 1);
            SupportedDate = SupportedDate.AddDays(int.Parse(Day) + 5);

            Log.Debug($"{SupportedDate.Year} {SupportedDate.Month} {SupportedDate.Day}");

            List<RoomBase> Base = RoomManager.AllRooms.Where(A => A.Tags.Contains("#base,") && A.MinSupportedDate <= SupportedDate).ToList();
            List<Models.MidLate2018.Room> Results = Base.Select(A => new RoomDetails(A).Room).ToList();
            return Results;
        }

        [Get("/api/rooms/v1/myrooms")]
        public List<FullRoom>? GetMyRoomsV1()
        {
            List<RoomBase> Mine = RoomManager.AllRooms.Where(A => A.CreatorPlayerId == long.Parse(Config.GetString("AccountId"))).ToList();
            List<FullRoom> Results = new List<FullRoom>();
            foreach (var Room in Mine)
            {
                Results.Add(new FullRoom(Room));
            }
            return Results;
        }

        [Get("/api/rooms/v2/mybookmarkedrooms")]
        public List<Models.MidLate2018.Room>? GetMyBookmarks()
        {
            if(RoomManager.AllRooms == null)
            {
                Log.Warn("invalid rooms list");
                return new List<Models.MidLate2018.Room>();
            }
            List<long> Bookmarks = RoomManager.GetBookmarks();
            List<Models.MidLate2018.Room> Results = new List<Models.MidLate2018.Room>();
            foreach (var Bookmark in Bookmarks)
            {
                RoomBase? Room = RoomManager.AllRooms.FirstOrDefault(A => A.RoomId == Bookmark);
                if(Room != null)
                {
                    Results.Add(new RoomDetails(Room).Room);
                }
                else
                {
                    Log.Warn("bookmarked invalid room");
                }
            }
            return Results;
        }

        [Get("/api/rooms/v1/mybookmarkedrooms")]
        public List<FullRoom>? GetMyBookmarksV1()
        {
            if (RoomManager.AllRooms == null)
            {
                Log.Warn("invalid rooms list");
                return new List<FullRoom>();
            }
            List<long> Bookmarks = RoomManager.GetBookmarks();
            List<FullRoom> Results = new List<FullRoom>();
            foreach (var Bookmark in Bookmarks)
            {
                RoomBase? Room = RoomManager.AllRooms.FirstOrDefault(A => A.RoomId == Bookmark);
                if (Room != null)
                {
                    Results.Add(new FullRoom(Room));
                }
                else
                {
                    Log.Warn("bookmarked invalid room");
                }
            }
            return Results;
        }

        [Get("/api/rooms/v2/myRecent")]
        public List<FullRoom>? GetMyRecent()
        {
            if (RoomManager.AllRooms == null)
            {
                Log.Warn("invalid rooms list");
                return new List<FullRoom>();
            }
            List<long> Recents = RoomManager.GetRecentRooms();
            List<FullRoom> Results = new List<FullRoom>();
            foreach (var Recent in Recents)
            {
                RoomBase? Room = RoomManager.AllRooms.FirstOrDefault(A => A.RoomId == Recent);
                if (Room != null)
                {
                    Results.Add(new FullRoom(Room));
                }
                else
                {
                    Log.Warn("invalid recent room");
                }
            }
            return Results;
        }

        [Get("/api/rooms/v2/mySubscriptions")]
        public List<FullRoom>? GetMySubscriptions()
        {
            return new List<FullRoom>();
        }

        [Get("/api/rooms/v2/search")]
        public List<Models.MidLate2018.Room>? SearchRooms()
        {
            string? Name = Context.Request.QueryString["value"];
            if(string.IsNullOrEmpty(Name))
            {
                return new List<Models.MidLate2018.Room>();
            }
            if(RoomManager.AllRooms == null)
            {
                Log.Warn("Invalid room list");
                return new List<Models.MidLate2018.Room>();
            }

            DateTime SupportedDate = DateTime.MinValue;

            if (string.IsNullOrEmpty(StartManager.GameVersion))
            {
                Log.Warn("Unknown game version for room filter!");
                return new List<Models.MidLate2018.Room>();
            }

            string Year = StartManager.GameVersion.Substring(0, 4);
            string Month = StartManager.GameVersion.Substring(4, 2);
            string Day = StartManager.GameVersion.Substring(6, 2);
            Log.Debug($"{Year} {Month} {Day}");

            SupportedDate = SupportedDate.AddYears(int.Parse(Year) - 1);
            SupportedDate = SupportedDate.AddMonths(int.Parse(Month) - 1);
            SupportedDate = SupportedDate.AddDays(int.Parse(Day) + 5);

            Log.Debug($"{SupportedDate.Year} {SupportedDate.Month} {SupportedDate.Day}");

            List<RoomBase> Rooms = RoomManager.AllRooms.Where(A => A.MinSupportedDate <= SupportedDate && A.Name != null && A.Name.ToLower().Contains(Name.ToLower()) && A.Accessibility == Accessibility.Public).ToList();
            List<Models.MidLate2018.Room> Results = Rooms.Select(A => new RoomDetails(A).Room).ToList();
            return Results;
        }

        [Post("//api/images/v3/uploadsaved")]
        public async Task<ImageResult> UploadImage()
        {
            byte[] Data = GetPostBytes();
            var Parse = MultipartFormDataParser.Parse(new MemoryStream(Data), boundary: Multipart.GetBoundary(Data, Context));

            FilePart F = Parse.Files[0];
            Log.Debug("image name: " + F.Name);
            MemoryStream ImageData = new MemoryStream();
            F.Data.CopyTo(ImageData);
            byte[] AAAA = ImageData.ToArray();
            string ImageName = "Image" + new Random().Next();
            FileManager.WriteBytes("Images/" + ImageName + ".png", AAAA);
            return new ImageResult()
            {
                ImageName = ImageName,
            };
        }

        [Post("//api/images/v4/uploadtransient")]
        public async Task<ImageResult> UploadImageTransient()
        {
            byte[] Data = GetPostBytes();
            var Parse = MultipartFormDataParser.Parse(new MemoryStream(Data), boundary: Multipart.GetBoundary(Data, Context));

            FilePart F = Parse.Files[0];
            Log.Debug("image name: " + F.Name);
            MemoryStream ImageData = new MemoryStream();
            F.Data.CopyTo(ImageData);
            byte[] AAAA = ImageData.ToArray();
            string ImageName = "Image" + new Random().Next();
            FileManager.WriteBytes("Images/" + ImageName + ".png", AAAA);
            return new ImageResult()
            {
                ImageName = ImageName,
            };
        }

        [Post("/api/rooms/v1/clone")]
        public Models.MidLate2018.CreateModifyRoomResponse? CloneRoom()
        {
            string PostData = GetPostString();
            RoomCloneRequest? CloneRequest = JsonSerializer.Deserialize<RoomCloneRequest>(PostData);
            if (CloneRequest == null)
            {
                Log.Warn("Invalid clone room request!");
                return null;
            }
            if (string.IsNullOrEmpty(CloneRequest.Name))
            {
                Log.Warn("Invalid name!");
                return null;
            }
            if (!WordFilter.IsPure(CloneRequest.Name))
            {
                return new Models.MidLate2018.CreateModifyRoomResponse()
                {
                    Result = LateRoomResult.InappropriateName
                };
            }
            if (RoomManager.AllRooms == null)
            {
                return new Models.MidLate2018.CreateModifyRoomResponse()
                {
                    Result = LateRoomResult.RoomDoesNotExist
                };
            }
            if (RoomManager.AllRooms.FirstOrDefault(A => A.Name != null && A.Name.ToLower() == CloneRequest.Name.ToLower()) != null)
            {
                return new Models.MidLate2018.CreateModifyRoomResponse()
                {
                    Result = LateRoomResult.DuplicateName
                };
            }

            RoomBase? Request = RoomManager.AllRooms.FirstOrDefault(A => A.RoomId == CloneRequest.RoomId);

            if (Request == null)
            {
                return new Models.MidLate2018.CreateModifyRoomResponse()
                {
                    Result = LateRoomResult.RoomDoesNotExist
                };
            }

            RoomBase New = new RoomBase()
            {
                AllowScreenMode = Request.AllowScreenMode,
                Sandbox = Request.Sandbox,
                CreationTime = DateTime.Now,
                CreatorPlayerId = int.Parse(LocalQuest.Config.GetString("AccountId")),
                Description = Request.Description,
                ImageName = Request.ImageName,
                Name = CloneRequest.Name,
                RoomId = new Random().Next(),
                Accessibility = Request.Accessibility,
                Scenes = Request.Scenes,
                MinSupportedDate = Request.MinSupportedDate,
                DormRoom = Request.DormRoom,
                RRO = false,
                Tags = Request.Tags,
            };
            RoomManager.AddLocalRoom(New);

            return new Models.MidLate2018.CreateModifyRoomResponse()
            {
                Result = LateRoomResult.Success,
                RoomDetails = new RoomDetails(New)
            };
        }

        [Post("/api/rooms/v1/saveData/{var}")]
        public async Task<FullRoom?> SaveRoom(string RoomId)
        {
            byte[] Data = GetPostBytes();
            var Parse = MultipartFormDataParser.Parse(new MemoryStream(Data), boundary: Multipart.GetBoundary(Data, Context));
            
            FilePart F = Parse.Files[0];
            MemoryStream RoomData = new MemoryStream();
            F.Data.CopyTo(RoomData);
            byte[] AAAA = RoomData.ToArray();
            string RoomName = "RoomFile" + new Random().Next();
            FileManager.WriteBytes("RoomData/" + RoomName + ".room", AAAA);
            if(RoomManager.AllRooms == null)
            {
                Log.Warn("No rooms found");
                return null;
            }
            RoomBase? Room = RoomManager.AllRooms.FirstOrDefault(A => A.RoomId == long.Parse(RoomId));
            if (Room == null)
            {
                Log.Warn("Room not found");
                return null;
            }
            Room.Scenes[0].DataBlob = RoomName;
            RoomManager.UpdateLocalRoom(Room);
            await Notify.SendNotification(new Notification()
            {
                Id = Models.MidLate2018.NotificationType.SubscriptionUpdateRoom,
                Msg = new FullRoom(Room)
            });
            return new FullRoom(Room);
        }

        [Post("/api/rooms/v2/saveData/{var}")]
        public async Task<RoomDetails?> SaveRoomV2(string RoomId)
        {
            byte[] Data = GetPostBytes();
            var Parse = MultipartFormDataParser.Parse(new MemoryStream(Data), boundary: Multipart.GetBoundary(Data, Context));

            FilePart F = Parse.Files[0];
            MemoryStream RoomData = new MemoryStream();
            F.Data.CopyTo(RoomData);
            byte[] AAAA = RoomData.ToArray();
            string RoomName = "RoomFile" + new Random().Next();
            FileManager.WriteBytes("RoomData/" + RoomName + ".room", AAAA);
            if (RoomManager.AllRooms == null)
            {
                Log.Warn("No rooms found");
                return null;
            }
            RoomBase? Room = RoomManager.AllRooms.FirstOrDefault(A => A.RoomId == long.Parse(RoomId));
            if (Room == null)
            {
                Log.Warn("Room not found");
                return null;
            }
            Room.Scenes[0].DataBlob = RoomName;
            RoomManager.UpdateLocalRoom(Room);
            await Notify.SendNotification(new Notification()
            {
                Id = Models.MidLate2018.NotificationType.SubscriptionUpdateRoom,
                Msg = new RoomDetails(Room)
            });
            return new RoomDetails(Room);
        }

        [Get("/api/rooms/v2/{var}")]
        public Models.MidLate2018.Room? GetRoom(string RoomId)
        {
            if (RoomManager.AllRooms == null)
            {
                Context.Response.StatusCode = 404;
                Log.Warn("no rooms found!");
                return null;
            }
            RoomBase? Room = RoomManager.AllRooms.FirstOrDefault(A => A.RoomId == long.Parse(RoomId));
            if (Room == null)
            {
                Context.Response.StatusCode = 404;
                Log.Warn("room not found!");
                return null;
            }
            return new RoomDetails(Room).Room;
        }

        [Get("/api/rooms/v1/filters")]
        public RoomFilters GetFilters()
        {
            return new RoomFilters();
        }

        [Post("/api/presence/v3/heartbeat")]
        public HeartbeatResponse Heartbeat()
        {
            return new HeartbeatResponse()
            {
                Error = "",
                Presence = Notify.CurrentPresence
            };
        }

        [Post("/api/rooms/v1/bookmark")]
        public SuccessResponse BookmarkRoom()
        {
            string PostContent = GetPostString();
            BookmarkRequest? Req = JsonSerializer.Deserialize<BookmarkRequest>(PostContent);
            if(Req == null)
            {
                return new SuccessResponse()
                {
                    Success = false,
                    Message = "invalid request [|X3]"
                };
            }
            RoomManager.BookmarkRoom(Req.RoomId, Req.Bookmark);
            return new SuccessResponse()
            {
                Success = true,
                Message = ""
            };
        }

        [Get("/api/rooms/v3/browse")]
        public List<FullRoom> BrowseRooms()
        {
            List<FullRoom> Results = new List<FullRoom>();
            if(RoomManager.AllRooms == null)
            {
                Log.Warn("no rooms found!");
                return Results;
            }
            DateTime SupportedDate = DateTime.MinValue;

            if(string.IsNullOrEmpty(StartManager.GameVersion))
            {
                Log.Warn("Unknown game version for room filter!");
                return Results;
            }

            string Year = StartManager.GameVersion.Substring(0, 4);
            Log.Debug("Year: " + Year);
            string Month = StartManager.GameVersion.Substring(4, 2);
            Log.Debug("Month: " + Month);
            string Day = StartManager.GameVersion.Substring(6, 2);
            Log.Debug("Day: " + Day);

            foreach (var Room in RoomManager.AllRooms.Where(A => A.Accessibility == Accessibility.Public))
            {
                Results.Add(new FullRoom(Room));
            }
            return Results;
        }

        [Get("/api/rooms/v1/hot")]
        public List<Models.MidLate2018.Room> HotRooms()
        {
            string? Tags = Context.Request.QueryString["Tags"];
            if (Tags != null)
            {
                Log.Debug(Tags);
            }
            List<Models.MidLate2018.Room> Results = new List<Models.MidLate2018.Room>();
            if (RoomManager.AllRooms == null)
            {
                Log.Warn("no rooms found!");
                return Results;
            }

            DateTime SupportedDate = DateTime.MinValue;

            if (string.IsNullOrEmpty(StartManager.GameVersion))
            {
                Log.Warn("Unknown game version for room filter!");
                return new List<Models.MidLate2018.Room>();
            }

            string Year = StartManager.GameVersion.Substring(0, 4);
            string Month = StartManager.GameVersion.Substring(4, 2);
            string Day = StartManager.GameVersion.Substring(6, 2);
            Log.Debug($"{Year} {Month} {Day}");

            SupportedDate = SupportedDate.AddYears(int.Parse(Year) - 1);
            SupportedDate = SupportedDate.AddMonths(int.Parse(Month) - 1);
            SupportedDate = SupportedDate.AddDays(int.Parse(Day) + 5);

            Log.Debug($"{SupportedDate.Year} {SupportedDate.Month} {SupportedDate.Day}");

            foreach (var Room in RoomManager.AllRooms.Where(A => A.MinSupportedDate <= SupportedDate && A.Accessibility == Accessibility.Public))
            {
                Results.Add(new RoomDetails(Room).Room);
            }
            return Results;
        }

        [Post("/api/rooms/v2/browse")]
        public List<FullRoom> BrowseRoomsV2()
        {
            List<FullRoom> Results = new List<FullRoom>();
            if (RoomManager.AllRooms == null)
            {
                Log.Warn("no rooms found!");
                return Results;
            }
            foreach (var Room in RoomManager.AllRooms.Where(A => A.Accessibility == Accessibility.Public))
            {
                Results.Add(new FullRoom(Room));
            }
            return Results;
        }

        [Post("/api/settings/v2/set")]
        public List<PlayerSetting>? SetSetting()
        {
            string PostData = GetPostString();

            List<PlayerSetting>? Settings = FileManager.GetJSON<List<PlayerSetting>>("PlayerData/Settings");

            // should never be null
            if(Settings == null)
            {
                return null;
            }

            PlayerSetting? NewSetting = JsonSerializer.Deserialize<PlayerSetting>(PostData);
            if (NewSetting == null)
            {
                return null;
            }

            PlayerSetting? Existing = Settings.FirstOrDefault(A => A.Key == NewSetting.Key);
            if (Existing != null)
            {
                Existing.Value = NewSetting.Value;
            }
            else
            {
                Settings.Add(NewSetting);
            }
            FileManager.WriteJSON("PlayerData/Settings", Settings);

            return Settings;
        }

        [Get("/api/groups/v1/{var}")]
        public Group GetGroup(string GroupId)
        {
            return new Group();
        }

        [Post("/api/images/v3/profile")]
        public async Task<SuccessResponse> UploadPFP()
        {
            byte[] Data = GetPostBytes();
            string PostData = Encoding.UTF8.GetString(Data);
            string Boundary = Context.Request.ContentType.Split("boundary=")[0].Replace("\"", "");
            byte[] BoundaryBytes = Encoding.UTF8.GetBytes("--" + Boundary);
            byte[] AAAA = Multipart.Read(Data, Context);
            string ImageName = "ProfilePicture" + new Random().Next();
            FileManager.WriteBytes("Images/" + ImageName + ".png", AAAA);
            Config.SetString("PFP", ImageName);
            await Notify.SendNotification(new Notification()
            {
                Id = Models.MidLate2018.NotificationType.SubscriptionUpdateProfile,
                Msg = new Profile()
            });
            return new SuccessResponse()
            {
                Success = true,
                Message = JsonSerializer.Serialize(new Profile())
            };
        }

        [Post("/api/players/v2/displayname")]
        public async Task<SuccessResponse> ChangeDisplayName()
        {
            if(Form == null)
            {
                return new SuccessResponse()
                {
                    Success = false,
                    Message = "invalid form"
                };
            }
            string? Name = Form["Name"];
            if (string.IsNullOrEmpty(Name))
            {
                return new SuccessResponse()
                {
                    Success = false,
                    Message = "invalid name"
                };
            }
            if (!WordFilter.IsPure(Name))
            {
                return new SuccessResponse()
                {
                    Success = false,
                    Message = "bad name"
                };
            }
            Config.SetString("DisplayName", Name);
            await Notify.SendNotification(new Notification()
            {
                Id = Models.MidLate2018.NotificationType.SubscriptionUpdateProfile,
                Msg = new Profile()
            });
            return new SuccessResponse()
            {
                Success = true,
                Message = JsonSerializer.Serialize(new Profile())
            };
        }

        [Post("/api/PlayerCheer/v1/SetSelectedCheer")]
        public async Task<SuccessResponse> SelectCheer()
        {
            if (Form == null)
            {
                return new SuccessResponse()
                {
                    Success = false,
                    Message = "invalid form"
                };
            }
            string? Category = Form["CheerCategory"];
            Log.Debug("Cheer Type: " + Category);
            if(string.IsNullOrEmpty(Category))
            {
                Config.SetInt("CheerCategory", null);
            }
            else
            {
                Config.SetInt("CheerCategory", int.Parse(Category));
            }
            await Notify.SendNotification(new Notification()
            {
                Id = Models.MidLate2018.NotificationType.SubscriptionUpdateProfile,
                Msg = new Profile()
            });
            return new SuccessResponse()
            {
                Success = true,
                Message = JsonSerializer.Serialize(new Profile())
            };
        }

        [Post("/api/players/v1/bio")]
        public async Task<SuccessResponse> ChangeBio()
        {
            if (Form == null)
            {
                return new SuccessResponse()
                {
                    Success = false,
                    Message = "invalid form"
                };
            }
            string? Bio = Form["bio"];
            if (string.IsNullOrEmpty(Bio))
            {
                return new SuccessResponse()
                {
                    Success = false,
                    Message = "invalid bio"
                };
            }
            if (!WordFilter.IsPure(Bio))
            {
                return new SuccessResponse()
                {
                    Success = false,
                    Message = "bad bio"
                };
            }
            Config.SetString("Bio", Bio);
            await Notify.SendNotification(new Notification()
            {
                Id = Models.MidLate2018.NotificationType.SubscriptionUpdateProfile,
                Msg = new Profile()
            });
            return new SuccessResponse()
            {
                Success = true,
                Message = JsonSerializer.Serialize(new Profile())
            };
        }

        [Post("//api/sanitize/v1/isPure")]
        public PureResponse IsPure()
        {
            string POST = GetPostString();
            PureRequest? Request = JsonSerializer.Deserialize<PureRequest>(POST);
            if(Request == null)
            {
                return new PureResponse()
                {
                    IsPure = false
                };
            }
            return new PureResponse()
            {
                IsPure = WordFilter.IsPure(Request.Value)
            };
        }

        [Get("/api/players/v1/{var}")]
        public Profile? GetProfileOld(string Id)
        {
            if(Id == Config.GetString("AccountId"))
            {
                return new Profile();
            }
            else
            {
                Context.Response.StatusCode = 404;
                return null;
            }
        }

        [Get("/api/events/v3/list")]
        public List<PlayerEvent> ListEvents()
        {
            return new List<PlayerEvent>();
        }

        [Post("/api/players/v1/list")]
        public List<Profile> ListPlayers()
        {
            string Post = GetPostString();
            List<long>? PlayerIds = JsonSerializer.Deserialize<List<long>>(Post);

            if(PlayerIds == null)
            {
                return new List<Profile>();
            }

            List<Profile> Results = new List<Profile>();

            if(PlayerIds.Contains(1))
            {
                Results.Add(new Profile()
                {
                    Username = "Coach",
                    DisplayName = "Coach",
                    ProfileImageName = "DefaultPFP",
                    Id = 1
                });
            }

            return Results;
        }

        [Get("/api/playersubscriptions/v1/my")]
        public List<Subscription> GetSubscriptions()
        {
            return new List<Subscription>();
        }

        [Get("/api/avatar/v2")]
        public PlayerAvatar? GetAvatar()
        {
            return FileManager.GetJSON<PlayerAvatar>("Avatar/LocalAvatar");
        }

        [Post("/api/avatar/v2/set")]
        public PlayerAvatar? SetAvatar()
        {
            string PostData = GetPostString();
            PlayerAvatar? NewAvatar = JsonSerializer.Deserialize<PlayerAvatar>(PostData);
            if(NewAvatar == null)
            {
                return null;
            }
            FileManager.WriteJSON("Avatar/LocalAvatar", NewAvatar);
            return NewAvatar;
        }

        [Get("/api/notification/v2")]
        public async Task Notifcation()
        {
            if(Context.Request.IsWebSocketRequest)
            {
                WebSocketContext WsContext = await Context.AcceptWebSocketAsync(subProtocol: null);
                Notify.ConnectNotify(WsContext);
            }    
        }
    }
}
