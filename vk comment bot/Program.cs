using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using vk_comment_bot;
using VkNet;
using VkNet.Enums.Filters;
using VkNet.Model;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;

class Program
{
    public static int GetHistoryCounter { get; set; } = 0;
    public static string StartTime { get; set; } = string.Empty;
    public static List<ModelHistory>? PostId { get; set; }
    public static List<VkApi> VkApis { get; set; } = new();
    public static GetNewsFeed GetNewsFeed { get; set; } = new();

    static void Main()
    {
        bool withAudio = File.ReadAllText("withAudio.txt") == "1";
        string[] tokens = File.ReadAllLines("tokens.txt");
        string[] keywords = File.ReadAllLines("keywords.txt");
        string audioFile = File.ReadAllText("audio.txt");
        string message = File.ReadAllText("message.txt");

        PostId = JsonConvert.DeserializeObject<List<ModelHistory>>(File.ReadAllText("his.txt")) ?? new List<ModelHistory>();

        WriteLineWithColor($"Чтение файлов завершено. Токинов в системе - {tokens.Length}\r\n" +
                           $"Ключевых слов - {keywords.Length}\r\n" +
                           $"Сообщение - {message}\r\n" +
                           $"Только с вложением - {withAudio}\r\n" +
                           $"История содержит записей - {PostId.Count}", ConsoleColor.White);

        AuthorizeTokens(tokens);

        if (InItAudio(audioFile, out Audio audio)) return;

        WriteLineWithColor($"Аудио получено. name - {audio.Title} artist - {audio.Artist}", ConsoleColor.White);

        foreach (var vkApi in VkApis)
        {
            WriteLineWithColor($"Используется токен {vkApi.Account.GetProfileInfoAsync().Result.FirstName}", ConsoleColor.White);
            for (int i = 0; i < 40;)
            {
                try
                {
                    WriteLineWithColor("Делаю поиск через 30 сек", ConsoleColor.Magenta);
                    Thread.Sleep(3000);
                    var searchResult = GetNewsFeed.GetNews(string.Join(" OR ", keywords), 30, StartTime, tokens[GetHistoryCounter % tokens.Length]);
                    JArray array = searchResult.response.items;
                    WriteLineWithColor($"Поиск завершен, постов - {array.Count}", ConsoleColor.White);
                    if (array.Count == 0)
                    {
                        WriteLineWithColor(
                            $"Поиск не выдал результата. Либо плохой запрос, либо ВК заблокировало функцию поиска для акка (появится снова через часов 5)\r\n" +
                            $"Желательно выключить бота, либо убрать невалидный токен", ConsoleColor.Red);
                    }


                    foreach (var post in searchResult?.response?.items)
                    {
                        ModelHistory history = GetModelHistory(post);
                        // проверка на историю комментов
                        if (PostId != null && PostId.Any(modelHistory => modelHistory == history))
                        {
                            WriteLineWithColor($"Пост уже был обработан", ConsoleColor.Yellow);
                            continue;
                        }

                        // проверка на аудио
                        if (withAudio && !HaveAudioAttachment(post))
                        {
                            WriteLineWithColor($"В посте нет вложения, пропуск. Если это сообщение появляется слишком часто, можно сделать withaudio.txt = 0", ConsoleColor.White);
                            continue;
                        }

                        PostId?.Add(history);
                        File.WriteAllText("his.txt", JsonConvert.SerializeObject(PostId));

                        if (CanPost(post))
                        {
                            if (Convert.ToInt64(history.OwnerId) < 0)
                            {
                                WriteLineWithColor("Пост принадлежит группе", ConsoleColor.Yellow);
                                continue;
                            }

                            try
                            {
                                vkApi.Wall.CreateComment(new WallCreateCommentParams
                                {
                                    OwnerId = post.owner_id,
                                    PostId = post.id,
                                    Message = string.IsNullOrWhiteSpace(message) ? "" : message,
                                    Attachments = new List<MediaAttachment>
                                {
                                    audio
                                }
                                });

                                WriteLineWithColor($"Комментарий оставлен {i + 1}", ConsoleColor.Green);
                            }
                            catch (Exception e)
                            {
                                WriteLineWithColor($"Ошибка при комментировании {e.Message}", ConsoleColor.Red);
                            }

                            i++;
                            Thread.Sleep(60000);
                        }
                        else
                        {
                            WriteLineWithColor("Комментарии закрыты", ConsoleColor.Yellow);
                        }

                        WriteLineWithColor($"Найден пост с ID {post.id} от {post.from_id}", ConsoleColor.Green);
                    }
                }
                catch (Exception e)
                {
                    WriteLineWithColor($"Ошибка при выполнении поиска постов: {e.Message}", ConsoleColor.Red);
                }
            }

            WriteLineWithColor($"Смена аккаунта", ConsoleColor.Yellow);
        }

        WriteLineWithColor($"Работа завершена, аккаунты закончились", ConsoleColor.Green);
        WriteLineWithColor($"Нажмите любую кнопку", ConsoleColor.White);
        Console.ReadKey();
    }

    private static void AuthorizeTokens(string[] tokens)
    {
        foreach (var t in tokens)
        {
            VkApi api = new VkApi();
            api.Authorize(new ApiAuthParams
            {
                AccessToken = t
            });
            VkApis.Add(api);
        }
    }

    private static bool InItAudio(string audioFile, out Audio audio)
    {
        audio = VkApis.First().Audio.GetById(new[] { audioFile }).FirstOrDefault();

        if (audio == null)
        {
            WriteLineWithColor("Ошибка при получении трека. Возможно, проблема в id или 1-й токен не работает", ConsoleColor.Red);
            WriteLineWithColor("Нажмите любую кнопку для выхода", ConsoleColor.White);
            Console.ReadKey();
            return true;
        }

        return false;
    }

    private static NewsSearchResult GetNewsSearchResult(string[] keywords, VkApi vkApi)
    {
        var searchParams = new NewsFeedSearchParams
        {
            Query = string.Join(" OR ", keywords),
            Extended = true,
            Count = 3,
            Fields = UsersFields.All,
            StartFrom = StartTime,
        };

        var searchResult = vkApi.NewsFeed.Search(searchParams);

        StartTime = searchResult.NextFrom;
        return searchResult;
    }

    private static void WriteLineWithColor(string text, ConsoleColor color)
    {
        var tempColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ForegroundColor = tempColor;
    }

    private static bool HaveAudioAttachment(dynamic item)
    {
        if (item == null) return false;
        if (item.attachments == null) return false;

        foreach (var attachment in item.attachments)
        {
            if (attachment.type == "audio")
                return true;
        }

        return false;
    }

    private static bool CanPost(dynamic item)
    {
        if (item.comments == null) return false;

        if (item.comments.can_post == "1") return true;
        return false;
    }

    private static ModelHistory GetModelHistory(dynamic item)
    {
        return new ModelHistory
        {
            OwnerId = item.owner_id,
            Date = item.date,
        };
    }
}