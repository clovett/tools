using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using BlogML;

namespace BlogMLToWordpress
{
    class Program
    {
        List<string> files = new List<string>();

        static void Main(string[] args)
        {
            Program p = new Program();
            if (!p.ParseCommandLine(args))
            {
                PrintUsage();
                return;
            }
            p.Convert();
        }

        private static void PrintUsage()
        {
            Console.WriteLine("Usage: BlogMLToWordpress BlogML.xml");
            Console.WriteLine("Converts the given BlogML.xml file to BlogWRX.xml format for importing into wordpress");
        }

        bool ParseCommandLine(string[] args)
        {
            for (int i = 0, n = args.Length; i < n; i++)
            {
                string arg = args[i];
                if (arg[0] == '-' || arg[1] == '/')
                {
                    switch (arg.Substring(1).ToLowerInvariant())
                    {
                        case "h":
                        case "help":
                        case "?":
                            return false;
                        default:
                            break;
                    }
                }
                else
                {
                    files.Add(arg);
                }

            }
            if (files.Count == 0)
            {
                return false;
            }
            return true;
        }

        private void Convert()
        {
            foreach (string file in files)
            {
                ConvertFile(file);
            }
        }

        private void ConvertFile(string file)
        {
            string dir = Path.GetDirectoryName(file);
            string baseName = Path.GetFileNameWithoutExtension(file);
            if (baseName.EndsWith("ML", StringComparison.InvariantCultureIgnoreCase))
            {
                baseName = baseName.Substring(0, baseName.Length - 2);
            }
            string outFile = Path.Combine(dir, baseName + "WRX.xml");
            Console.WriteLine("Converting {0} to {1}", file, outFile);

            BlogML.blogType blogData = null;
            XmlSerializer serializer = new XmlSerializer(typeof(BlogML.blogType));
            using (TextReader reader = new StreamReader(file))
            {
                blogData = (BlogML.blogType)serializer.Deserialize(reader);
                reader.Close();
            }

            XmlWriterSettings ws = new XmlWriterSettings();
            ws.Indent = true;
            using (XmlWriter writer = XmlWriter.Create(outFile, ws))
            {
                Convert(blogData, writer);
            }

        }

        static string dcns = "http://purl.org/dc/elements/1.1/";
        static string wpns = "http://wordpress.org/export/1.0/";
        static string contentNs = "http://purl.org/rss/1.0/modules/content/";

        private void Convert(blogType blogData, XmlWriter writer)
        {
            int postNumber = 0;
            int commentId = 200;

            writer.WriteStartDocument();
            writer.WriteStartElement("rss");
            writer.WriteAttributeString("version", "2.0");
            writer.WriteAttributeString("xmlns", "content", null, contentNs);
            writer.WriteAttributeString("xmlns", "wfw", null, "http://wellformedweb.org/CommentAPI/");
            writer.WriteAttributeString("xmlns", "dc", null, dcns);
            writer.WriteAttributeString("xmlns", "wp", null, wpns);

            // Write Blog Info.
            writer.WriteStartElement("channel");
            writer.WriteElementString("title", String.Join(" ", string.Join(" ", blogData.title.Text)));
            writer.WriteElementString("link", blogData.rooturl);
            writer.WriteElementString("description", (blogData.subtitle != null && blogData.subtitle.Text != null) ? string.Join(" ", blogData.subtitle.Text) : "");
            writer.WriteElementString("pubDate", blogData.datecreated.ToString("ddd, dd MMM yyyy HH:mm:ss +0000"));
            writer.WriteElementString("generator", "http://wordpress.org/?v=MU");
            writer.WriteElementString("language", "en");
            writer.WriteElementString("wp", "wxr_version", wpns, "1.0");
            writer.WriteElementString("wp", "base_site_url", wpns, blogData.rooturl);
            writer.WriteElementString("wp", "base_blog_url", wpns, blogData.rooturl);

            // Create tags 
            foreach (var tag in GetUniqueTags(blogData))
            {
                writer.WriteStartElement("wp", "tag", wpns);
                writer.WriteElementString("wp", "tag_slug", wpns, tag.Replace(' ', '-'));
                writer.WriteStartElement("wp", "tag_name", wpns);
                writer.WriteCData(tag);
                writer.WriteEndElement(); // wp:tag_name
                writer.WriteEndElement(); // sp:tag
            }

            Dictionary<string, string> categories = new Dictionary<string, string>();

            // Create categories
            if (blogData.categories != null)
            {
                for (int i = 0; i <= blogData.categories.Length - 1; i++)
                {
                    categoryType currCategory = blogData.categories[i];
                    string name = string.Join(" ", currCategory.title.Text).ToLower().Replace(' ', '-');
                    string content = string.Join(" ", currCategory.title.Text);
                    categories[currCategory.id] = content;
                    writer.WriteStartElement("wp", "category", wpns);
                    writer.WriteElementString("wp", "category_nicename", wpns, name);
                    //writer.WriteElementString("wp", "category_parent", wpns, "");
                    writer.WriteStartElement("wp", "cat_name", wpns);
                    writer.WriteCData(content);
                    writer.WriteEndElement(); // wp:cat_name
                    writer.WriteEndElement(); // wp:category
                }
            }

            for (int i = 0; i <= blogData.posts.Length - 1; i++)
            {
                postType currPost = blogData.posts[i];

                writer.WriteStartElement("item");
                writer.WriteElementString("title", string.Join(" ", currPost.title.Text));
                writer.WriteElementString("link", currPost.posturl);
                writer.WriteElementString("pubDate", currPost.datecreated.ToString("ddd, dd MMM yyyy HH:mm:ss +0000"));
                writer.WriteStartElement("dc", "creator", dcns);
                writer.WriteCData(String.Join(" ", blogData.authors.author.title.Text));
                writer.WriteEndElement(); // dc:creator

                // Post Tags/Categories (currently only categories are implemented with BlogML
                if (currPost.categories != null)
                {
                    for (int j = 0; j <= currPost.categories.Length - 1; j++)
                    {
                        categoryRefType currCatRef = currPost.categories[j];
                        string categoryName = "";
                        categories.TryGetValue(currCatRef.@ref, out categoryName);
                        writer.WriteStartElement("category");
                        writer.WriteCData(categoryName);
                        writer.WriteEndElement(); // category
                        writer.WriteStartElement("category");
                        writer.WriteAttributeString("domain", "category");
                        writer.WriteAttributeString("nicename", categoryName.ToLower().Replace(' ', '-'));
                        writer.WriteCData(categoryName);
                        writer.WriteEndElement(); // category domain=category
                    }
                }
                if (currPost.tags != null)
                {
                    for (int j = 0; j <= currPost.tags.Length - 1; j++)
                    {
                        tagRefType currtagRef = currPost.tags[j];
                        string tagName = currtagRef.@ref;
                        writer.WriteStartElement("category");
                        writer.WriteCData(tagName);
                        writer.WriteEndElement(); // category
                        writer.WriteStartElement("category");
                        writer.WriteAttributeString("domain", "post_tag");
                        writer.WriteAttributeString("nicename", tagName.ToLower().Replace(' ', '-'));
                        writer.WriteCData(tagName);
                        writer.WriteEndElement(); // category domain=category
                    }
                }
                
                writer.WriteStartElement("guid");
                writer.WriteAttributeString("isPermaLink", "false");
                writer.WriteString(currPost.id);
                writer.WriteEndElement(); // guid
                writer.WriteElementString("description", ".");
                writer.WriteStartElement("encoded", contentNs);
                writer.WriteCData(currPost.content.Value);
                writer.WriteEndElement(); // content:encoded
                writer.WriteElementString("wp", "post_id", wpns, postNumber.ToString());
                postNumber++;
                writer.WriteElementString("wp", "post_date", wpns, currPost.datecreated.ToString("yyyy-MM-dd HH:mm:ss"));
                writer.WriteElementString("wp", "post_date_gmt", wpns, currPost.datecreated.ToString("yyyy-MM-dd HH:mm:ss"));
                writer.WriteElementString("wp", "comment_status", wpns, "open");
                writer.WriteElementString("wp", "ping_status", wpns, "open");
                writer.WriteElementString("wp", "post_name", wpns, string.Join(" ", currPost.title.Text).ToLower().Replace(' ', '-'));
                writer.WriteElementString("wp", "status", wpns, "publish");
                writer.WriteElementString("wp", "post_parent", wpns, "0");
                writer.WriteElementString("wp", "menu_order", wpns, "0");
                writer.WriteElementString("wp", "post_type", wpns, "post");
                writer.WriteStartElement("wp", "post_password", wpns);
                writer.WriteString(" ");
                writer.WriteEndElement(); // wp:post_password

                if (currPost.comments != null)
                {
                    for (int k = 0; k <= currPost.comments.Length - 1; k++)
                    {
                        commentType currComment = currPost.comments[k];
                        writer.WriteStartElement("wp", "comment", wpns);
                        writer.WriteElementString("wp", "comment_id", wpns, commentId++.ToString());
                        commentId++;
                        writer.WriteElementString("wp", "comment_date", wpns, currComment.datecreated.ToString("yyyy-MM-dd HH:mm:ss"));
                        writer.WriteElementString("wp", "comment_date_gmt", wpns, currComment.datecreated.ToString("yyyy-MM-dd HH:mm:ss"));
                        writer.WriteStartElement("wp", "comment_author", wpns);
                        if ((!String.IsNullOrEmpty(currComment.useremail)) || (currComment.useremail != "http://"))
                        {
                            writer.WriteCData(currComment.username);
                        }
                        else
                        {
                            writer.WriteCData("Nobody");
                        }
                        writer.WriteEndElement(); // wp:comment_author
                        writer.WriteElementString("wp", "comment_author_email", wpns, currComment.useremail);
                        writer.WriteElementString("wp", "comment_author_url", wpns, currComment.userurl);
                        writer.WriteElementString("wp", "comment_type", wpns, " ");
                        writer.WriteStartElement("wp", "comment_content", wpns);
                        writer.WriteCData(currComment.content.Value);
                        writer.WriteEndElement(); // wp:comment_content

                        if (currComment.approved)
                        {
                            writer.WriteElementString("wp", "comment_approved", wpns, "1");
                        }
                        else
                        {
                            writer.WriteElementString("wp", "comment_approved", wpns, "0");
                        }

                        writer.WriteElementString("wp", "comment_parent", "0");
                        writer.WriteEndElement(); // wp:comment
                    }
                }

                writer.WriteEndElement(); // item


            }

            writer.WriteEndElement(); // channel
            writer.WriteEndElement(); // rss

            writer.Flush();
            writer.Close();
        }


        IEnumerable<string> GetUniqueTags(blogType blogData)
        {
            HashSet<string> unique = new HashSet<string>();
            if (blogData.posts != null)
            {
                foreach (var post in blogData.posts)
                {
                    if (post.tags != null)
                    {
                        foreach (var tag in post.tags)
                        {
                            if (!string.IsNullOrEmpty(tag.@ref))
                            {
                                unique.Add(tag.@ref);
                            }
                        }
                    }
                }
            }
            return unique;
        }
    }
}
