namespace AutoBingSearch.Core.Search;

public static class QueryBank
{
    private static readonly string[] Queries =
    [
        "world news today",
        "how to build an emergency fund",
        "quick 30 minute dinner ideas",
        "benefits of yoga for mental health",
        "best habits of successful people",
        "what was the Renaissance period",
        "classic movies everyone should watch",
        "how does GPS work",
        "best indoor plants for low light",
        "how to improve sleep quality naturally",
        "what is antimatter",
        "healthy breakfast ideas",
        "how to organize a small desk",
        "history of democracy in ancient Greece",
        "best budget travel tips",
        "how to make fried rice at home",
        "difference between affect and effect",
        "what does pragmatic mean",
        "how to negotiate a salary raise",
        "best ways to insulate a home",
        "how to reduce inflammation naturally",
        "NFL draft picks 2025",
        "boxing fight schedule 2025",
        "Wimbledon 2025 draw",
        "how to set and achieve goals",
        "what human foods can dogs eat",
        "easy dessert recipes with few ingredients",
        "best countries to visit in South America",
        "how to set up a NAS drive",
        "best fish for beginners aquarium",
        "fun facts about the ocean",
        "how to winterize your home",
        "benefits of intermittent fasting",
        "what does tangential mean",
        "best paint colors for living room 2026",
        "how to budget money effectively",
        "top rated shows on streaming 2026",
        "Olympics 2028 sports list",
        "best safari destinations in Africa",
        "what is machine learning in simple terms",
        "how to learn touch typing",
        "best exercises for posture",
        "how to make sourdough starter",
        "space telescope discoveries",
        "how do solar panels work",
        "best note taking methods",
        "how to improve focus while studying",
        "what is compound interest",
        "how to clean a mechanical keyboard",
        "best free photo editing software",
        "how to start journaling",
        "simple home workout routine",
        "how to cook lentils",
        "best productivity books",
        "what causes thunderstorms",
        "how to create a personal budget",
        "beginner guitar practice routine",
        "how to make cold brew coffee",
        "best ways to learn a language",
        "what is quantum computing"
    ];

    public static IReadOnlyList<string> PickDailyQueries(int count, DateOnly? date = null)
    {
        count = Math.Clamp(count, 1, 30);
        var seedDate = date ?? DateOnly.FromDateTime(DateTime.Today);
        var rng = new Random(seedDate.DayNumber);
        return Queries
            .OrderBy(_ => rng.Next())
            .Take(count)
            .ToList();
    }
}
