using SaintKnight.Day20;
using SaintKnight.Day26;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace SaintKnight.Rest
{
    /// <summary>
    /// 2026-11-22 (user): the Silver Hare salon split in two village shops, both walked through in first person
    /// (SKShop): the beastkin bar "꼬리 등불" (Greta, Sorel, Lili) and the bunny girl's gambling hall "은토끼 도박장"
    /// (Rune). Beastkin hunted by the corrupted church hid under the town; once the Madonna falls they open their doors.
    /// Talking a host through to the end makes her closer (salon bonds; friends join the hot spring).
    /// Original characters (all adults) and writing for this project; 15+ wording here.
    /// </summary>
    public static class SKTavern
    {
        public const string Folder = "SaintKnightRest/Private/";
        // 루네 (24, bunny-girl dealer), 그레타 (23, golden-retriever bartender), 소렐 (31, fox owner, information broker),
        // 릴리 (25, rabbit beastkin stage dancer)
        public const string R = "루네", G = "그레타", S = "소렐", L = "릴리", C = "세실리아", A = "아델라";
        // 2026-11-22 (user: more shops and characters, all women): 노엘라 (dark elf croupier), 바르가 (wolf bouncer),
        // 미라 (witch apothecary), 힐데 (oni blacksmith), 카엘라 (elf apprentice)
        public const string N = "노엘라", V = "바르가", M = "미라", H = "힐데", K = "카엘라";

        public static bool Unlocked => SKHotSpring.Unlocked;

        // 2026-11-19 user: the hosts join the hot spring only after you get close to them by talking.
        // Hearing a host's talk to the end counts once per shop visit; three such visits make her a friend.
        public const int FriendTalks = 3;
        public static int Bond(string host) => (SKSave.Data.salonBonds ?? new string[0]).Count(h => h == host);
        public static bool IsFriend(string host) => Bond(host) >= FriendTalks;
        public static bool IsHost(string who) => who == R || who == G || who == S || who == L;

        public static int AddBond(string host)
        {
            var d = SKSave.Data;
            d.salonBonds = (d.salonBonds ?? new string[0]).Concat(new[] { host }).ToArray();
            SKSave.Write();
            return Bond(host);
        }

        private static string[] T(string who, string text, string picture = null) =>
            picture == null ? new[] { who, text } : new[] { who, text, picture };
        private static Func<int, string[][]> Talks(params string[][][] talks) =>
            n => n == 0 || talks.Length == 1 ? talks[0] : talks[1 + (n - 1) % (talks.Length - 1)];

        // ---------- the beastkin bar ----------

        public static readonly string[][] GretaFirst =
        {
            T(G, "어서 오세요~ 꼬리 등불에! 수인 손님이든 사람 손님이든 여기선 다 같은 단골이에요!"),
            T(C, "꼬리 모양 등불이 걸린 집이 여기였군요."),
            T(G, "맞아요! 오늘은 뭐로 드릴까요? 제 추천은 꿀 넣은 데운 포도주!", "salon_greta_mix1"),
            T(A, "그걸로 두 잔."),
            T(G, "흔들흔들~ 짠, 나왔습니다!", "salon_greta_serve"),
            T(C, "…꼬리가 계속 흔들려요."),
            T(G, "헤헤, 손님이 좋으면 멋대로 그래요. 못 본 척해 주세요!"),
        };
        public static readonly string[][] GretaAgain =
        {
            T(G, "오늘은 한가해서 카운터에 좀 엎드려 있을래요~", "salon_greta_lie"),
            T(A, "바텐더가 그래도 돼?"),
            T(G, "사장님이 안 보시면 괜찮아요! …지금 보고 계시나요?"),
            T(G, "아, 아니다! 한 잔 더 섞어 드릴게요!", "salon_greta_mix2"),
        };
        public static readonly string[][] SorelFirst =
        {
            T(S, "꼬리 등불 주인, 소렐이야. 성당이 수인을 사냥하던 시절부터 이 지하를 지켜 왔지."),
            T(C, "…교단이 여러분을 쫓았던 거군요. 죄송해요."),
            T(S, "사과는 됐어. 대신 이야기 하나를 팔지.", "salon_sorel_wine"),
            T(S, "교단 장부에 ‘다음 그릇’이라는 말이 있었어. 성모가 마지막에 한 말과 같지?"),
            T(A, "그 장부는 어디 있지?"),
            T(S, "성녀들의 납골당. 수로 벽에 긁힌 꼬리 자국 아래에 샛문이 있어.", "salon_sorel_serve1"),
        };
        public static readonly string[][] SorelAgain =
        {
            T(S, "값은 이미 받았어. 너희가 살아서 이 문을 다시 여는 것.", "salon_sorel_serve2"),
            T(C, "…꼭 돌아올게요."),
            T(S, "약속은 술로 하는 거야. 한 모금이면 돼."),
        };
        public static readonly string[][] LiliFirst =
        {
            T(L, "사슬 언니, 장검 언니! 구경만 할 거야? 릴리 옆자리 비어 있어."),
            T(A, "무대는 언제 시작하는데?"),
            T(L, "언니들이 앉으면 바로! 토끼 귀는 박자를 절대 안 놓치거든.", "salon_lili_above"),
            T(C, "귀로 박자를요…?"),
            T(L, "보여 줄까? 하나, 둘… 앗, 넘어진다!", "salon_lili_lean"),
        };
        public static readonly string[][] LiliAgain =
        {
            T(L, "오늘은 뒤에서 몰래 다가가는 연습 중이야.", "salon_lili_back"),
            T(A, "들켰어."),
            T(L, "칫. 그럼 오늘 밤은 무대 맨 앞자리에서 봐야 해!", "salon_lili_low"),
        };

        public static readonly string[][] GretaHot =
        {
            T(G, "으앗, 쏟았다…! 손님 잔에 따르다가 그만 제 쪽으로…", "salon_greta_lean"),
            T(C, "괘, 괜찮아요? 다 젖었어요."),
            T(G, "헤헤… 끈적끈적해요. 닦는 거 도와주실래요?"),
            T(A, "…수건 가져올게."),
        };
        public static readonly string[][] SorelPipe =
        {
            T(S, "오늘은 장사 접었어. 이 시간엔 담뱃대 하나랑 너희면 충분하지.", "salon_sorel_pipe"),
            T(A, "그 차림으로 손님을 받는 거야?"),
            T(S, "손님은 너희뿐이잖아. 그리고 이게 내 잠옷이야."),
            T(C, "…눈을 어디 둬야 할지 모르겠어요."),
        };
        public static readonly string[][] LiliStage =
        {
            T(L, "오늘 무대는 언니들 전용이야! 끝까지 눈 떼면 안 돼!", "salon_lili_stage"),
            T(A, "봉은 언제 설치한 거야."),
            T(L, "어제! 그레타 언니가 휘청거리다가 박살 낼 뻔했지만~"),
            T(C, "바, 박수 쳐도 되나요?"),
        };
        public static readonly string[][] VargaFirst =
        {
            T(V, "멈춰. …아, 성모를 쓰러뜨린 둘이구나. 들어가.", "salon_varga"),
            T(A, "문지기치곤 순순하네."),
            T(V, "사냥꾼은 냄새로 알아. 너희한텐 피 냄새는 나도 교단 냄새는 안 나."),
            T(C, "고마워요."),
            T(V, "고마우면 나중에 한 판 붙어 줘. 요즘 몸이 근질근질하거든."),
        };
        public static readonly string[][] VargaAgain =
        {
            T(V, "또 왔어? 오늘은 내가 한 잔 살게."),
            T(A, "문은 어쩌고."),
            T(V, "그레타가 꼬리로 지키겠지, 뭐."),
        };

        public static readonly SKShopDef BeastBar = new SKShopDef
        {
            Title = "꼬리 등불",
            Subtitle = "사냥을 피해 숨은 수인들의 지하 술집",
            Establishing = "beastbar_entrance",
            Main = new SKShopRoom
            {
                Picture = "salon_bg",
                Spots = new[]
                {
                    new SKShopSpot { Label = "문가의 늑대 문지기", At = new Vector2(.17f, .4f), Host = V, Picture = "salon_varga", Talk = Talks(VargaFirst, VargaAgain) },
                    new SKShopSpot { Label = "바텐더에게 간다", At = new Vector2(.31f, .5f), Host = G, Picture = "salon_greta", Talk = Talks(GretaFirst, GretaAgain, GretaHot) },
                    new SKShopSpot { Label = "안쪽 자리의 여주인", At = new Vector2(.55f, .3f), Host = S, Picture = "salon_sorel", Talk = Talks(SorelFirst, SorelAgain, SorelPipe) },
                    new SKShopSpot { Label = "무대 옆 소파", At = new Vector2(.8f, .5f), Host = L, Picture = "salon_lili", Talk = Talks(LiliFirst, LiliAgain, LiliStage) },
                    new SKShopSpot { Label = "촛불 탁자를 살펴본다", At = new Vector2(.04f, .8f), Talk = Talks(new[]
                    {
                        T(null, "촛농이 겹겹이 쌓인 둥근 탁자. 발톱 자국과 이빨 자국이 나란히 패어 있다."),
                        T(A, "싸운 자국인 줄 알았는데… 건배할 때마다 잔을 깨문 자국이래."),
                    }) },
                },
            },
        };

        // ---------- the bunny girl's gambling hall ----------

        public static readonly string[][] RuneFirst =
        {
            T(null, "카운터 아래로 삐죽 나온 토끼 귀가 움찔한다."),
            T(R, "…들켰다. 숨어 있다가 놀래 주려고 했는데."),
            T(R, "어서 와, 은토끼 도박장에. 여긴 운이랑 배짱만 있으면 누구든 손님이야."),
            T(C, "성소 바로 아래에 도박장이라니…"),
            T(R, "교단이 문 닫으라고 할 때마다 자리만 옮겼지. 판돈은 술 한 잔, 진 사람이 마시는 거야.", "salon_rune_serve"),
            T(A, "규칙이 간단해서 좋네."),
            T(R, "탁자는 오른쪽. 카드 판 앞에 앉으면 내가 갈게~"),
        };
        public static readonly string[][] RuneAgain =
        {
            T(R, "오늘 운은 어때? 토끼 귀를 만지면 운이 붙는다는 말도 있는데.", "salon_rune_flush"),
            T(C, "그, 그런 말은 처음 들어요."),
            T(R, "방금 내가 지어냈으니까~"),
        };

        private static readonly string[] CardNames = { "A", "2", "3", "4", "5", "6", "7", "8", "9", "10", "J", "Q", "K" };
        /// <summary>Rune's losses this session: her losing picture goes one step further each time (lose1..3).</summary>
        public static int RuneLosses;
        /// <summary>The draw, (Cecilia's card, Rune's card), 0 = A .. 12 = K. Tests pin it.</summary>
        public static Func<(int, int)> Draw = () => (UnityEngine.Random.Range(0, 13), UnityEngine.Random.Range(0, 13));

        public static string[][] CardRound(int round)
        {
            var (p, r) = Draw();
            var lines = new List<string[]>
            {
                T(R, round == 0 ? "한 장씩 뒤집어서 높은 쪽이 이겨. 진 사람이 한 잔!" : "한 판 더? 좋아, 섞는다~", "salon_rune_deal"),
                T(null, "세실리아의 카드는 " + CardNames[p] + ", 루네의 카드는 " + CardNames[r] + "."),
            };
            if (p > r)
            {
                RuneLosses++;
                lines.Add(T(R, RuneLosses == 1 ? "으앗, 졌다… 약속은 약속이니까, 한 잔!" : "또야? 오늘 성녀님 운이 너무 좋은데…",
                    "salon_rune_lose" + Mathf.Min(RuneLosses, 3)));
                lines.Add(T(R, RuneLosses >= 3 ? "…취하면 옷매무새가 엉망이 돼. 못 본 척해 줘." : "다음 판엔 안 봐줄 거야."));
            }
            else if (p < r)
            {
                lines.Add(T(R, "후훗, 내 승리~ 자, 세실리아가 마실 차례!", "salon_rune_win"));
                lines.Add(T(C, "…달아요. 그런데 목이 뜨거워요."));
            }
            else lines.Add(T(R, "같은 숫자? 그럼 둘 다 마시는 걸로!", "salon_rune_serve"));
            return lines.ToArray();
        }

        public static readonly string[][] NoelaFirst =
        {
            T(N, "루네의 손님이구나. 나는 룰렛 담당 노엘라.", "salon_noela"),
            T(C, "다크 엘프 분은 처음 봬요."),
            T(N, "숲에서 쫓겨나고 성당에서 쫓겨나고, 결국 여기서 돈을 굴리고 있지."),
            T(A, "운이 좋은 편인가?"),
            T(N, "운은 없어. 대신 손이 빠르지. 카드 한 장 받아 볼래?"),
        };
        public static readonly string[][] NoelaAgain =
        {
            T(N, "오늘은 내가 딜러야. 루네는 지난 판에 너무 마셔서 쉬는 중."),
            T(C, "괜찮은 거예요?"),
            T(N, "카운터 밑에서 자고 있어. 귀만 삐죽 나와 있을걸."),
        };

        public static readonly SKShopDef Gamble = new SKShopDef
        {
            Title = "은토끼 도박장",
            Subtitle = "판돈은 술 한 잔, 진 사람이 마신다",
            Establishing = "gamble_entrance",
            Main = new SKShopRoom
            {
                Picture = "Gamble/bg_bar_counter",
                Spots = new[]
                {
                    new SKShopSpot { Label = "카운터 아래 토끼 귀", At = new Vector2(.5f, .9f), Host = R, Picture = "salon_rune", Talk = Talks(RuneFirst, RuneAgain) },
                    new SKShopSpot { Label = "술병 선반을 본다", At = new Vector2(.3f, .42f), Talk = Talks(new[]
                    {
                        T(null, "분홍빛으로 빛나는 술병이 끝없이 늘어서 있다. 라벨마다 토끼 발자국 도장이 찍혀 있다."),
                        T(A, "판돈으로 걸리는 술인가. …독하겠네."),
                    }) },
                    new SKShopSpot { Label = "룰렛 딜러에게 간다", At = new Vector2(.7f, .6f), Host = N, Picture = "salon_noela", Talk = Talks(NoelaFirst, NoelaAgain) },
                    new SKShopSpot { Label = "스테인드글라스 등불", At = new Vector2(.08f, .14f), Talk = Talks(new[]
                    {
                        T(null, "성당 창유리를 깨서 만든 등불이 천천히 흔들린다."),
                        T(C, "이 문양, 성소 회랑 창이랑 같아요. 누가 몰래 옮겨 왔나 봐요."),
                    }) },
                    new SKShopSpot
                    {
                        Label = "도박 탁자로 간다", At = new Vector2(.9f, .62f),
                        Room = new SKShopRoom
                        {
                            Picture = "Gamble/bg_table_top",
                            Spots = new[]
                            {
                                new SKShopSpot { Label = "루네와 카드 한 판", At = new Vector2(.62f, .62f), Host = R, Picture = "salon_rune_table", Talk = CardRound },
                                new SKShopSpot { Label = "룰렛과 주사위", At = new Vector2(.18f, .3f), Talk = Talks(new[]
                                {
                                    T(null, "금빛 룰렛이 달그락거리며 돈다. 구슬은 붉은 칸에서 멈췄다."),
                                    T(C, "붉은색… 성모의 장미창 색이에요."),
                                    T(null, "옆의 상아 주사위 한쪽 면에는 토끼 귀가 새겨져 있다."),
                                    T(A, "그 면이 나오면 딜러가 이기는 건가. 영리하네."),
                                }) },
                                new SKShopSpot { Label = "분홍 샷 잔", At = new Vector2(.82f, .27f), Talk = Talks(new[]
                                {
                                    T(null, "분홍빛 술이 찰랑이는 샷 잔이 다섯 개. 진 사람의 몫이다."),
                                    T(C, "다섯 잔이면… 끝까지 지면 다 마셔야 하는 거죠?"),
                                }) },
                            },
                        },
                    },
                },
            },
        };

        // ---------- 2026-11-22 new shops ----------

        public static readonly SKShopDef Apothecary = new SKShopDef
        {
            Title = "달빛 약방",
            Subtitle = "기분이 좋아지는 약도 판다는 마녀의 가게",
            Establishing = "apothecary_entrance",
            Main = new SKShopRoom
            {
                Picture = "apothecary_room",
                Spots = new[]
                {
                    new SKShopSpot { Label = "끓는 가마솥", At = new Vector2(.17f, .72f), Talk = Talks(new[]
                    {
                        T(null, "보랏빛 물이 부글부글 끓는다. 장미 꽃잎과 무언가의 비늘이 떠 있다."),
                        T(A, "냄새만 맡아도 머리가 핑 도네."),
                    }) },
                    new SKShopSpot { Label = "카운터의 마녀", At = new Vector2(.5f, .55f), Host = M, Picture = "shop_mira", Talk = Talks(new[]
                    {
                        T(M, "어서 와, 달빛 약방이야. 성녀님이 직접 오다니, 오늘은 별이 잘 맞나 봐."),
                        T(C, "상처에 바를 약을 찾고 있어요."),
                        T(M, "상처약도 있고… 기분이 좋아지는 약도 있지. 시음은 공짜야."),
                        T(A, "그 분홍색은 뭔데."),
                        T(M, "후훗, 마셔 보면 알아. 대신 오늘 밤 일은 책임 안 져."),
                    }, new[]
                    {
                        T(M, "또 왔네? 지난번 그 약, 효과 좋았지?"),
                        T(C, "…기억이 잘 안 나요."),
                        T(M, "그럼 효과가 좋았던 거야."),
                    }) },
                    new SKShopSpot { Label = "약병 선반", At = new Vector2(.86f, .4f), Talk = Talks(new[]
                    {
                        T(null, "라벨마다 손글씨: 용기, 잠, 고백. ‘고백’ 칸만 유난히 비어 있다."),
                        T(C, "…고백이 제일 잘 팔리네요."),
                    }) },
                },
            },
        };

        public static readonly SKShopDef Forge = new SKShopDef
        {
            Title = "붉은 모루",
            Subtitle = "밤에도 불이 꺼지지 않는 대장간",
            Establishing = "forge_entrance",
            Main = new SKShopRoom
            {
                Picture = "forge_room",
                Spots = new[]
                {
                    new SKShopSpot { Label = "작업대의 견습생", At = new Vector2(.12f, .6f), Host = K, Picture = "shop_kaela", Talk = Talks(new[]
                    {
                        T(K, "아, 손님…! 잠깐만요, 지금 꼴이 말이 아니라서…"),
                        T(A, "견습생이야?"),
                        T(K, "엘프가 대장장이가 되겠다니까 다들 웃었어요. 힐데 언니만 빼고요."),
                        T(C, "멋져요. 불 앞이 덥지 않아요?"),
                        T(K, "더워요… 그래서 옷이 자꾸 이렇게 젖어요. 보, 보지 마세요!"),
                    }, new[]
                    {
                        T(K, "오늘 처음으로 못 하나를 혼자 만들었어요!"),
                        T(A, "어디 봐."),
                        T(K, "…휘었네요. 그래도 언니가 칭찬해 줬어요."),
                    }) },
                    new SKShopSpot { Label = "타오르는 화로", At = new Vector2(.35f, .45f), Talk = Talks(new[]
                    {
                        T(null, "숯이 붉게 숨 쉰다. 가까이 가자 얼굴이 금세 달아오른다."),
                        T(A, "여기서 하루 종일 일하면 옷이 남아나질 않겠네."),
                    }) },
                    new SKShopSpot { Label = "모루 앞의 대장장이", At = new Vector2(.56f, .75f), Host = H, Picture = "shop_hilde", Talk = Talks(new[]
                    {
                        T(H, "붉은 모루에 온 걸 환영해! 그 장검, 날이 많이 상했는데?"),
                        T(A, "성모의 촉수를 몇 번 베었거든."),
                        T(H, "하하! 오늘 밤 안에 새것처럼 만들어 주지. 대신 망치질 구경은 공짜가 아니야."),
                        T(C, "얼마인가요?"),
                        T(H, "땀 닦을 수건 한 장. 보다시피 여기 엄청 덥거든."),
                    }, new[]
                    {
                        T(H, "왔구나. 이번엔 사슬 고리도 손봐 줄까?"),
                        T(C, "부탁해요. 끊어진 고리가 하나 있어요."),
                        T(H, "끊어진 사슬은 다시 이으면 전보다 단단해져. 사람도 그렇고."),
                    }) },
                    new SKShopSpot { Label = "갑옷 진열대", At = new Vector2(.87f, .45f), Talk = Talks(new[]
                    {
                        T(null, "흑철 갑옷 사이에 장미 문양 방패가 하나 걸려 있다."),
                        T(A, "…교단 기사단 방패다. 여기까지 흘러왔구나."),
                    }) },
                },
            },
        };

        /// <summary>A warm lantern glow breathing over the door, so the shop reads without text.</summary>
        public static void MarkEntrance(Transform parent, float x)
        {
            var go = new GameObject("Tavern_Lantern");
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, -1.9f, 0f);
            var glow = go.AddComponent<SpriteRenderer>();
            glow.sprite = SKUi.Glow;
            glow.sortingOrder = 5;
            go.AddComponent<SKLanternGlow>();
        }
    }

    /// <summary>
    /// Dust drifting through the lantern light over the salon CG (reference reel: fine specks floating in the air).
    /// Each speck is one art pixel and snaps to the picture's pixel grid. Only on salon pictures.
    /// </summary>
    public sealed class SKDustMotes : MonoBehaviour
    {
        public const int Count = 26;
        private RawImage raw;
        private readonly List<Image> motes = new List<Image>();
        private readonly List<Vector4> state = new List<Vector4>();   // x, y (art px), speed, phase

        public static SKDustMotes Attach(RawImage target)
        {
            if (target == null) return null;
            var d = target.GetComponent<SKDustMotes>();
            return d != null ? d : target.gameObject.AddComponent<SKDustMotes>();
        }

        public int Visible { get; private set; }

        private void Awake()
        {
            raw = GetComponent<RawImage>();
            for (int i = 0; i < Count; i++)
            {
                var m = new GameObject("Dust").AddComponent<Image>();
                m.transform.SetParent(transform, false);
                m.sprite = SKUi.White;
                m.raycastTarget = false;
                var r = m.rectTransform;
                r.anchorMin = r.anchorMax = r.pivot = Vector2.zero;
                motes.Add(m);
                state.Add(new Vector4(Random.value, Random.value, Random.Range(.6f, 1.6f), Random.value * 10f));
            }
        }

        private void LateUpdate()
        {
            var tex = raw.texture;
            bool on = tex != null && (tex.name.StartsWith("salon_") || tex.name.StartsWith("bg_") || tex.name.EndsWith("_entrance"));
            Visible = 0;
            var size = raw.rectTransform.rect.size;
            float t = Time.unscaledTime;
            for (int i = 0; i < motes.Count; i++)
            {
                motes[i].enabled = on;
                if (!on) continue;
                var s = state[i];
                // drift up slowly with a lazy sideways sway; wrap at the top
                float y = Mathf.Repeat(s.y + t * s.z * .012f, 1f);
                float x = Mathf.Repeat(s.x + Mathf.Sin(t * .4f + s.w) * .02f + t * .003f, 1f);
                float px = size.x / tex.width, py = size.y / tex.height;
                var r = motes[i].rectTransform;
                r.sizeDelta = new Vector2(px, py);
                r.anchoredPosition = new Vector2(Mathf.Floor(x * tex.width) * px, Mathf.Floor(y * tex.height) * py);
                float twinkle = .5f + .5f * Mathf.Sin(t * (1.3f + s.z) + s.w * 3f);
                motes[i].color = new Color(1f, .88f, .66f, Mathf.Lerp(.08f, .55f, twinkle * twinkle));
                Visible++;
            }
        }
    }

    public sealed class SKLanternGlow : MonoBehaviour
    {
        private SpriteRenderer r;
        private void Awake() => r = GetComponent<SpriteRenderer>();
        private void Update()
        {
            float k = .5f + .5f * Mathf.Sin(Time.unscaledTime * 2.3f) * Mathf.Sin(Time.unscaledTime * .7f + 1f);
            transform.localScale = Vector3.one * (2.2f + .3f * k);
            r.color = new Color(1f, .72f, .38f, .22f + .12f * k);
        }
    }
}
