using System;
using System.Collections.Generic;
using System.Threading;
using Neko.Drawing;

namespace Neko.Sources.APIS;

public class TheCatAPI : ImageSource
{
    public class Config : IImageConfig
    {
        public bool enabled;
        public Breed breed = Breed.All;
        public int selected;

        public ImageSource? LoadConfig() => enabled ? new TheCatAPI(breed) : null;
    }

    public override string Name => "TheCatAPI";

    private const int URL_COUNT = 10;
    private readonly MultiURLs<TheCatAPIJson> URLs;
    private readonly Breed breed;

    public TheCatAPI(Breed breed = Breed.All)
    {
        var baseUrl = $"https://api.thecatapi.com/v1/images/search?limit={URL_COUNT}";

        this.breed = breed;
        if (breed == Breed.All)
        {
            URLs = new(baseUrl, this);
            return;
        }

        var info = GetBreedInfo(breed);
        URLs = new(baseUrl + $"&breed_ids={info.ID}", this);
    }

    public override NekoImage Next(CancellationToken ct = default)
    {
        return new NekoImage(async (img) =>
        {
            var url = await URLs.GetURL(ct);
            img.URLDownloadWebsite = url;
            return await Download.DownloadImage(url, typeof(TheCatAPI), ct);
        }, this);
    }

    public static BreedInfo GetBreedInfo(Breed b)
    {
        return b == Breed.All
            ? throw new Exception("Cannot get BreedInfo for All breeds")
            : !BreedDictionary.TryGetValue(b, out var info)
            ? throw new Exception("Breed not in Database")
            : info;
    }

    public override string ToString() => $"TheCatAPI\tBreed: {Plugin.Config.Sources.TheCatAPI.breed}\t{URLs}";

    public override bool SameAs(ImageSource other) => other is TheCatAPI cat && cat.breed == breed;

#pragma warning disable

    public class TheCatAPIJson : List<TheCatAPIJson.Entry>, IJsonToList<string>
    {
        public class Entry
        {
            public string id { get; set; }
            public string url { get; set; }
            public int? width { get; set; }
            public int? height { get; set; }
        }
        public List<string> ToList()
        {
            List<string> res = new();
            foreach (var entry in this)
            {
                res.Add(entry.url);
            }
            return res;
        }
    }

    public enum Breed
    {
        All,
        Abyssinian, Aegean, American_Bobtail, American_Curl, American_Shorthair, American_Wirehair, Arabian_Mau, Australian_Mist,
        Balinese, Bambino, Bengal, Birman, Bombay, British_Longhair, British_Shorthair, Burmese, Burmilla,
        California_Spangled, Chantilly_Tiffany, Chartreux, Chausie, Cheetoh, Colorpoint_Shorthair, Cornish_Rex, Cymric, Cyprus,
        Devon_Rex, Donskoy, Dragon_Li,
        Egyptian_Mau, European_Burmese, Exotic_Shorthair,
        Havana_Brown, Himalayan,
        Japanese_Bobtail, Javanese,
        Khao_Manee, Korat, Kurilian,
        LaPerm,
        Maine_Coon, Malayan, Manx, Munchkin,
        Nebelung, Norwegian_Forest_Cat,
        Ocicat, Oriental,
        Persian, Pixie_bob,
        Ragamuffin, Ragdoll, Russian_Blue,
        Savannah, Scottish_Fold, Selkirk_Rex, Siamese, Siberian, Singapura, Snowshoe, Somali, Sphynx,
        Tonkinese, Toyger, Turkish_Angora, Turkish_Van,
        York_Chocolate
    }

    public struct BreedInfo
    {
        public string Name;
        public string Description;
        public string ID;

        public BreedInfo(string id, string name, string desc)
        {
            Name = name;
            Description = desc;
            ID = id;
        }
    }

    public static readonly Dictionary<Breed, BreedInfo> BreedDictionary = new() {
        {Breed.Abyssinian,           new BreedInfo("abys","Abyssinian","The Abyssinian is easy to care for, and a joy to have in your home. They’re affectionate cats and love both people and other animals.")},
        {Breed.Aegean,               new BreedInfo("aege","Aegean","Native to the Greek islands known as the Cyclades in the Aegean Sea, these are natural cats, meaning they developed without humans getting involved in their breeding. As a breed, Aegean Cats are rare, although they are numerous on their home islands. They are generally friendly toward people and can be excellent cats for families with children.")},
        {Breed.American_Bobtail,     new BreedInfo("abob","American Bobtail","American Bobtails are loving and incredibly intelligent cats possessing a distinctive wild appearance. They are extremely interactive cats that bond with their human family with great devotion.")},
        {Breed.American_Curl,        new BreedInfo("acur","American Curl","Distinguished by truly unique ears that curl back in a graceful arc, offering an alert, perky, happily surprised expression, they cause people to break out into a big smile when viewing their first Curl. Curls are very people-oriented, faithful, affectionate soulmates, adjusting remarkably fast to other pets, children, and new situations.")},
        {Breed.American_Shorthair,   new BreedInfo("asho","American Shorthair","The American Shorthair is known for its longevity, robust health, good looks, sweet personality, and amiability with children, dogs, and other pets.")},
        {Breed.American_Wirehair,    new BreedInfo("awir","American Wirehair","The American Wirehair tends to be a calm and tolerant cat who takes life as it comes. His favorite hobby is bird-watching from a sunny windowsill, and his hunting ability will stand you in good stead if insects enter the house.")},
        {Breed.Arabian_Mau,          new BreedInfo("amau","Arabian Mau","Arabian Mau cats are social and energetic. Due to their energy levels, these cats do best in homes where their owners will be able to provide them with plenty of playtime, attention and interaction from their owners. These kitties are friendly, intelligent, and adaptable, and will even get along well with other pets and children.")},
        {Breed.Australian_Mist,      new BreedInfo("amis","Australian Mist","The Australian Mist thrives on human companionship. Tolerant of even the youngest of children, these friendly felines enjoy playing games and being part of the hustle and bustle of a busy household. They make entertaining companions for people of all ages, and are happy to remain indoors between dusk and dawn or to be wholly indoor pets.")},
        {Breed.Balinese,             new BreedInfo("bali","Balinese","Balinese are curious, outgoing, intelligent cats with excellent communication skills. They are known for their chatty personalities and are always eager to tell you their views on life, love, and what you’ve served them for dinner. ")},
        {Breed.Bambino,              new BreedInfo("bamb","Bambino","The Bambino is a breed of cat that was created as a cross between the Sphynx and the Munchkin breeds. The Bambino cat has short legs, large upright ears, and is usually hairless. They love to be handled and cuddled up on the laps of their family members.")},
        {Breed.Bengal,               new BreedInfo("beng","Bengal","Bengals are a lot of fun to live with, but they're definitely not the cat for everyone, or for first-time cat owners. Extremely intelligent, curious and active, they demand a lot of interaction and woe betide the owner who doesn't provide it.")},
        {Breed.Birman,               new BreedInfo("birm","Birman","The Birman is a docile, quiet cat who loves people and will follow them from room to room. Expect the Birman to want to be involved in what you’re doing. He communicates in a soft voice, mainly to remind you that perhaps it’s time for dinner or maybe for a nice cuddle on the sofa. He enjoys being held and will relax in your arms like a furry baby.")},
        {Breed.Bombay,               new BreedInfo("bomb","Bombay","The the golden eyes and the shiny black coa of the Bopmbay is absolutely striking. Likely to bond most with one family member, the Bombay will follow you from room to room and will almost always have something to say about what you are doing, loving attention and to be carried around, often on his caregiver's shoulder.")},
        {Breed.British_Longhair,     new BreedInfo("bslo","British Longhair","The British Longhair is a very laid-back relaxed cat, often perceived to be very independent although they will enjoy the company of an equally relaxed and likeminded cat. They are an affectionate breed, but very much on their own terms and tend to prefer to choose to come and sit with their owners rather than being picked up.")},
        {Breed.British_Shorthair,    new BreedInfo("bsho","British Shorthair","The British Shorthair is a very pleasant cat to have as a companion, ans is easy going and placid. The British is a fiercely loyal, loving cat and will attach herself to every one of her family members. While loving to play, she doesn't need hourly attention. If she is in the mood to play, she will find someone and bring a toy to that person. The British also plays well by herself, and thus is a good companion for single people.")},
        {Breed.Burmese,              new BreedInfo("bure","Burmese","Burmese love being with people, playing with them, and keeping them entertained. They crave close physical contact and abhor an empty lap. They will follow their humans from room to room, and sleep in bed with them, preferably under the covers, cuddled as close as possible. At play, they will turn around to see if their human is watching and being entertained by their crazy antics.")},
        {Breed.Burmilla,             new BreedInfo("buri","Burmilla","The Burmilla is a fairly placid cat. She tends to be an easy cat to get along with, requiring minimal care. The Burmilla is affectionate and sweet and makes a good companion, the Burmilla is an ideal companion to while away a lonely evening. Loyal, devoted, and affectionate, this cat will stay by its owner, always keeping them company.")},
        {Breed.California_Spangled,  new BreedInfo("cspa","California Spangled","Perhaps the only thing about the California spangled cat that isn’t wild-like is its personality. Known to be affectionate, gentle and sociable, this breed enjoys spending a great deal of time with its owners. They are very playful, often choosing to perch in high locations and show off their acrobatic skills.")},
        {Breed.Chantilly_Tiffany,    new BreedInfo("ctif","Chantilly-Tiffany","The Chantilly is a devoted companion and prefers company to being left alone. While the Chantilly is not demanding, she will \"chirp\" and \"talk\" as if having a conversation. This breed is affectionate, with a sweet temperament. It can stay still for extended periods, happily lounging in the lap of its loved one. This quality makes the Tiffany an ideal traveling companion, and an ideal house companion for senior citizens and the physically handicapped.")},
        {Breed.Chartreux,            new BreedInfo("char","Chartreux","The Chartreux is generally silent but communicative. Short play sessions, mixed with naps and meals are their perfect day. Whilst appreciating any attention you give them, they are not demanding, content instead to follow you around devotedly, sleep on your bed and snuggle with you if you’re not feeling well.")},
        {Breed.Chausie,              new BreedInfo("chau","Chausie","For those owners who desire a feline capable of evoking the great outdoors, the strikingly beautiful Chausie retains a bit of the wild in its appearance but has the house manners of our friendly, familiar moggies. Very playful, this cat needs a large amount of space to be able to fully embrace its hunting instincts.")},
        {Breed.Cheetoh,              new BreedInfo("chee","Cheetoh","The Cheetoh has a super affectionate nature and real love for their human companions; they are intelligent with the ability to learn quickly. You can expect that a Cheetoh will be a fun-loving kitty who enjoys playing, running, and jumping through every room in your house.")},
        {Breed.Colorpoint_Shorthair, new BreedInfo("csho","Colorpoint Shorthair","Colorpoint Shorthairs are an affectionate breed, devoted and loyal to their people. Sensitive to their owner’s moods, Colorpoints are more than happy to sit at your side or on your lap and purr words of encouragement on a bad day. They will constantly seek out your lap whenever it is open and in the moments when your lap is preoccupied they will stretch out in sunny spots on the ground.")},
        {Breed.Cornish_Rex,          new BreedInfo("crex","Cornish Rex","This is a confident cat who loves people and will follow them around, waiting for any opportunity to sit in a lap or give a kiss. He enjoys being handled, making it easy to take him to the veterinarian or train him for therapy work. The Cornish Rex stay in kitten mode most of their lives and well into their senior years. ")},
        {Breed.Cymric,               new BreedInfo("cymr","Cymric","The Cymric is a placid, sweet cat. They do not get too upset about anything that happens in their world. They are loving companions and adore people. They are smart and dexterous, capable of using his paws to get into cabinets or to open doors.")},
        {Breed.Cyprus,               new BreedInfo("cypr","Cyprus","Loving, loyal, social and inquisitive, the Cyprus cat forms strong ties with their families and love nothing more than to be involved in everything that goes on in their surroundings. They are not overly active by nature which makes them the perfect companion for people who would like to share their homes with a laid-back relaxed feline companion. ")},
        {Breed.Devon_Rex,            new BreedInfo("drex","Devon Rex","The favourite perch of the Devon Rex is right at head level, on the shoulder of her favorite person. She takes a lively interest in everything that is going on and refuses to be left out of any activity. Count on her to stay as close to you as possible, occasionally communicating his opinions in a quiet voice. She loves people and welcomes the attentions of friends and family alike.")},
        {Breed.Donskoy,              new BreedInfo("dons","Donskoy","Donskoy are affectionate, intelligent, and easy-going. They demand lots of attention and interaction. The Donskoy also gets along well with other pets. It is now thought the same gene that causes degrees of hairlessness in the Donskoy also causes alterations in cat personality, making them calmer the less hair they have.")},
        {Breed.Dragon_Li,            new BreedInfo("lihu","Dragon Li","The Dragon Li is loyal, but not particularly affectionate. They are known to be very intelligent, and their natural breed status means that they're very active. She is is gentle with people, and has a reputation as a talented hunter of rats and other vermin.")},
        {Breed.Egyptian_Mau,         new BreedInfo("emau","Egyptian Mau","The Egyptian Mau is gentle and reserved. She loves her people and desires attention and affection from them but is wary of others. Early, continuing socialization is essential with this sensitive and sometimes shy cat, especially if you plan to show or travel with her. Otherwise, she can be easily startled by unexpected noises or events.")},
        {Breed.European_Burmese,     new BreedInfo("ebur","European Burmese","The European Burmese is a very affectionate, intelligent, and loyal cat. They thrive on companionship and will want to be with you, participating in everything you do. While they might pick a favorite family member, chances are that they will interact with everyone in the home, as well as any visitors that come to call. They are inquisitive and playful, even as adults. ")},
        {Breed.Exotic_Shorthair,     new BreedInfo("esho","Exotic Shorthair","The Exotic Shorthair is a gentle friendly cat that has the same personality as the Persian. They love having fun, don’t mind the company of other cats and dogs, also love to curl up for a sleep in a safe place. Exotics love their own people, but around strangers they are cautious at first. Given time, they usually warm up to visitors.")},
        {Breed.Havana_Brown,         new BreedInfo("hbro","Havana Brown","The Havana Brown is human oriented, playful, and curious. She has a strong desire to spend time with her people and involve herself in everything they do. Being naturally inquisitive, the Havana Brown reaches out with a paw to touch and feel when investigating curiosities in its environment. They are truly sensitive by nature and frequently gently touch their human companions as if they are extending a paw of friendship.")},
        {Breed.Himalayan,            new BreedInfo("hima","Himalayan","Calm and devoted, Himalayans make excellent companions, though they prefer a quieter home. They are playful in a sedate kind of way and enjoy having an assortment of toys. The Himalayan will stretch out next to you, sleep in your bed and even sit on your lap when she is in the mood.")},
        {Breed.Japanese_Bobtail,     new BreedInfo("jbob","Japanese Bobtail","The Japanese Bobtail is an active, sweet, loving and highly intelligent breed. They love to be with people and play seemingly endlessly. They learn their name and respond to it. They bring toys to people and play fetch with a favorite toy for hours. Bobtails are social and are at their best when in the company of people. They take over the house and are not intimidated. If a dog is in the house, Bobtails assume Bobtails are in charge.")},
        {Breed.Javanese,             new BreedInfo("java","Javanese","Javanese are endlessly interested, intelligent and active. They tend to enjoy jumping to great heights, playing with fishing pole-type or other interactive toys and just generally investigating their surroundings. He will attempt to copy things you do, such as opening doors or drawers.")},
        {Breed.Khao_Manee,           new BreedInfo("khao","Khao Manee","The Khao Manee is highly intelligent, with an extrovert and inquisitive nature, however they are also very calm and relaxed, making them an idea lap cat.")},
        {Breed.Korat,                new BreedInfo("kora","Korat","The Korat is a natural breed, and one of the oldest stable cat breeds. They are highly intelligent and confident cats that can be fearless, although they are startled by loud sounds and sudden movements. Korats form strong bonds with their people and like to cuddle and stay nearby.")},
        {Breed.Kurilian,             new BreedInfo("kuri","Kurilian","The character of the Kurilian Bobtail is independent, highly intelligent, clever, inquisitive, sociable, playful, trainable, absent of aggression and very gentle. They are devoted to their humans and when allowed are either on the lap of or sleeping in bed with their owners.")},
        {Breed.LaPerm,               new BreedInfo("lape","LaPerm","LaPerms are gentle and affectionate but also very active. Unlike many active breeds, the LaPerm is also quite content to be a lap cat. The LaPerm will often follow your lead; that is, if they are busy playing and you decide to sit and relax, simply pick up your LaPerm and sit down with it, and it will stay in your lap, devouring the attention you give it.")},
        {Breed.Maine_Coon,           new BreedInfo("mcoo","Maine Coon","They are known for their size and luxurious long coat Maine Coons are considered a gentle giant. The good-natured and affable Maine Coon adapts well to many lifestyles and personalities. She likes being with people and has the habit of following them around, but isn’t needy. Most Maine Coons love water and they can be quite good swimmers.")},
        {Breed.Malayan,              new BreedInfo("mala","Malayan","Malayans love to explore and even enjoy traveling by way of a cat carrier. They are quite a talkative and rather loud cat with an apparent strong will. These cats will make sure that you give it the attention it seeks and always seem to want to be held and hugged. They will constantly interact with people, even strangers. They love to play and cuddle.")},
        {Breed.Manx,                 new BreedInfo("manx","Manx","The Manx is a placid, sweet cat that is gentle and playful. She never seems to get too upset about anything. She is a loving companion and adores being with people.")},
        {Breed.Munchkin,             new BreedInfo("munc","Munchkin","The Munchkin is an outgoing cat who enjoys being handled. She has lots of energy and is faster and more agile than she looks. The shortness of their legs does not seem to interfere with their running and leaping abilities.")},
        {Breed.Nebelung,             new BreedInfo("nebe","Nebelung","The Nebelung may have a reserved nature, but she loves to play (being especially fond of retrieving) and enjoys jumping or climbing to high places where she can study people and situations at her leisure before making up her mind about whether she wants to get involved.")},
        {Breed.Norwegian_Forest_Cat, new BreedInfo("norw","Norwegian Forest Cat","The Norwegian Forest Cat is a sweet, loving cat. She appreciates praise and loves to interact with her parent. She makes a loving companion and bonds with her parents once she accepts them for her own. She is still a hunter at heart. She loves to chase toys as if they are real. She is territorial and patrols several times each day to make certain that all is fine.")},
        {Breed.Ocicat,               new BreedInfo("ocic","Ocicat","Loyal and devoted to their owners, the Ocicat is intelligent, confident, outgoing, and seems to have many dog traits. They can be trained to fetch toys, walk on a lead, taught to 'speak', come when called, and follow other commands. ")},
        {Breed.Oriental,             new BreedInfo("orie","Oriental","Orientals are passionate about the people in their lives. They become extremely attached to their humans, so be prepared for a lifetime commitment. When you are not available to entertain her, an Oriental will divert herself by jumping on top of the refrigerator, opening drawers, seeking out new hideaways.")},
        {Breed.Persian,              new BreedInfo("pers","Persian","Persians are sweet, gentle cats that can be playful or quiet and laid-back. Great with families and children, they absolutely love to lounge around the house. While they don’t mind a full house or active kids, they’ll usually hide when they need some alone time.")},
        {Breed.Pixie_bob,            new BreedInfo("pixi","Pixie-bob","Companionable and affectionate, the Pixie-bob wants to be an integral part of the family. The Pixie-Bob’s ability to bond with their humans along with their patient personas make them excellent companions for children.")},
        {Breed.Ragamuffin,           new BreedInfo("raga","Ragamuffin","The Ragamuffin is calm, even tempered and gets along well with all family members. Changes in routine generally do not upset her. She is an ideal companion for those in apartments, and with children due to her patient nature.")},
        {Breed.Ragdoll,              new BreedInfo("ragd","Ragdoll","Ragdolls love their people, greeting them at the door, following them around the house, and leaping into a lap or snuggling in bed whenever given the chance. They are the epitome of a lap cat, enjoy being carried and collapsing into the arms of anyone who holds them.")},
        {Breed.Russian_Blue,         new BreedInfo("rblu","Russian Blue","Russian Blues are very loving and reserved. They do not like noisy households but they do like to play and can be quite active when outdoors. They bond very closely with their owner and are known to be compatible with other pets.")},
        {Breed.Savannah,             new BreedInfo("sava","Savannah","Savannah is the feline version of a dog. Actively seeking social interaction, they are given to pouting if left out. Remaining kitten-like through life. Profoundly loyal to immediate family members whilst questioning the presence of strangers. Making excellent companions that are loyal, intelligent and eager to be involved.")},
        {Breed.Scottish_Fold,        new BreedInfo("sfol","Scottish Fold","The Scottish Fold is a sweet, charming breed. She is an easy cat to live with and to care for. She is affectionate and is comfortable with all members of her family. Her tail should be handled gently. Folds are known for sleeping on their backs, and for sitting with their legs stretched out and their paws on their belly. This is called the \"Buddha Position\".")},
        {Breed.Selkirk_Rex,          new BreedInfo("srex","Selkirk Rex","The Selkirk Rex is an incredibly patient, loving, and tolerant breed. The Selkirk also has a silly side and is sometimes described as clownish. She loves being a lap cat and will be happy to chat with you in a quiet voice if you talk to her. ")},
        {Breed.Siamese,              new BreedInfo("siam","Siamese","While Siamese cats are extremely fond of their people, they will follow you around and supervise your every move, being talkative and opinionated. They are a demanding and social cat, that do not like being left alone for long periods.")},
        {Breed.Siberian,             new BreedInfo("sibe","Siberian","The Siberians dog like temperament and affection makes the ideal lap cat and will live quite happily indoors. Very agile and powerful, the Siberian cat can easily leap and reach high places, including the tops of refrigerators and even doors. ")},
        {Breed.Singapura,            new BreedInfo("sing","Singapura","The Singapura is usually cautious when it comes to meeting new people, but loves attention from his family so much that she sometimes has the reputation of being a pest. This is a highly active, curious and affectionate cat. She may be small, but she knows she’s in charge")},
        {Breed.Snowshoe,             new BreedInfo("snow","Snowshoe","The Snowshoe is a vibrant, energetic, affectionate and intelligent cat. They love being around people which makes them ideal for families, and becomes unhappy when left alone for long periods of time. Usually attaching themselves to one person, they do whatever they can to get your attention.")},
        {Breed.Somali,               new BreedInfo("soma","Somali","The Somali lives life to the fullest. He climbs higher, jumps farther, plays harder. Nothing escapes the notice of this highly intelligent and inquisitive cat. Somalis love the company of humans and other animals.")},
        {Breed.Sphynx,               new BreedInfo("sphy","Sphynx","The Sphynx is an intelligent, inquisitive, extremely friendly people-oriented breed. Sphynx commonly greet their owners  at the front door, with obvious excitement and happiness. She has an unexpected sense of humor that is often at odds with her dour expression.")},
        {Breed.Tonkinese,            new BreedInfo("tonk","Tonkinese","Intelligent and generous with their affection, a Tonkinese will supervise all activities with curiosity. Loving, social, active, playful, yet content to be a lap cat")},
        {Breed.Toyger,               new BreedInfo("toyg","Toyger","The Toyger has a sweet, calm personality and is generally friendly. He's outgoing enough to walk on a leash, energetic enough to play fetch and other interactive games, and confident enough to get along with other cats and friendly dogs.")},
        {Breed.Turkish_Angora,       new BreedInfo("tang","Turkish Angora","This is a smart and intelligent cat which bonds well with humans. With its affectionate and playful personality the Angora is a top choice for families. The Angora gets along great with other pets in the home, but it will make clear who is in charge, and who the house belongs to")},
        {Breed.Turkish_Van,          new BreedInfo("tvan","Turkish Van","While the Turkish Van loves to jump and climb, play with toys, retrieve and play chase, she is is big and ungainly; this is one cat who doesn’t always land on his feet. While not much of a lap cat, the Van will be happy to cuddle next to you and sleep in your bed. ")},
        {Breed.York_Chocolate,       new BreedInfo("ycho","York Chocolate","York Chocolate cats are known to be true lap cats with a sweet temperament. They love to be cuddled and petted. Their curious nature makes them follow you all the time and participate in almost everything you do, even if it's related to water: unlike many other cats, York Chocolates love it.")}
    };

#pragma warning restore
}
