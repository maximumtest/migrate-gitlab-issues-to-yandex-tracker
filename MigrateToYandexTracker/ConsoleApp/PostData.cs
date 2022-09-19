using System.Collections.Generic;

namespace ConsoleApp
{
    public class PostData
    {
        public PostData(string queue)
        {
            Queue = queue;
        }

        public string Summary { get; set; }
        public string Queue { get; set; }
        public string Description { get; set; }
        public string StoryPoints { get; set; }
        public List<string> Tags { get; set; }
    }

    public class TagToDel
    {
        public string Tag { get; set; }
    }
}