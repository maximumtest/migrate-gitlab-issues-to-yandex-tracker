using System.Linq;

namespace ConsoleApp
{
    public static class Mapper
    {
        public static PostData Map(this PostData postData, RecivedData data)
        {
            postData.Summary = data.Title;
            postData.Description = data.Description;
            postData.StoryPoints = data.Weight;
            postData.Tags = string.IsNullOrEmpty(data.Labels) ? null : data.Labels.Split(",").Select(x => x.Replace("::", ": ")).ToList();

            return postData;
        }
    }
}