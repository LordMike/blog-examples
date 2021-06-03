using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace ef_composite_key_filtering
{
    class Program
    {
        static void Main(string[] args)
        {
            {
                BlogContext context = GetDatabase();

                if (context.Database.EnsureCreated())
                {
                    context.Posts.Add(new BlogPost { Author = "Mike", Title = "Intro to mysql" });
                    context.Posts.Add(new BlogPost { Author = "Mike", Title = "How the world works" });
                    context.Posts.Add(new BlogPost { Author = "John", Title = "Intro to mysql" });

                    context.SaveChanges();
                }
            }

            // The goal here is to select two of the three blogposts, using a query
            // Option 1: Build an expression for a known set of values. This does not support lists.
            {
                BlogContext context = GetDatabase();

                IQueryable<BlogPost> query = context.Posts.Where(s =>
                    (s.Author == "Mike" && s.Title == "Intro to mysql") ||
                    (s.Author == "Mike" && s.Title == "How the world works"));

                List<BlogPost> data = query.ToList();
                Console.WriteLine("Option 1:");
                Console.WriteLine(" Posts fetched: " + data.Count);
                Console.WriteLine(" Expression: " + query.Expression);
                Console.WriteLine(" SQL: " + query.ToQueryString());
                Console.WriteLine();
            }

            // Option 2: Build an expression with an unknown set of values. This requires expression building.
            {
                BlogContext context = GetDatabase();

                IQueryable<BlogPost> query = context.Posts;
                (string author, string title)[] values = { ("Mike", "Intro to mysql"), ("Mike", "How the world works") };

                {
                    // The expression we want to mimic is:
                    // post => (post.Type = a && post.Name = b) || (post.Type = c && post.Name = d) || ...
                    ParameterExpression postArgument = Expression.Parameter(typeof(BlogPost), "post");

                    MemberExpression propertyAuthor = Expression.Property(postArgument, nameof(BlogPost.Author));
                    MemberExpression propertyTitle = Expression.Property(postArgument, nameof(BlogPost.Title));

                    IEnumerable<BinaryExpression> ands = values
                        .Select(s =>
                        {
                            ConstantExpression constantAuthor = Expression.Constant(s.author);
                            ConstantExpression constantTitle = Expression.Constant(s.title);

                            // Expression: post.Type = a && post.Name = b
                            BinaryExpression andExpr = Expression.AndAlso(
                                Expression.Equal(propertyAuthor, constantAuthor),
                                Expression.Equal(propertyTitle, constantTitle));

                            return andExpr;
                        });

                    // Combine all the AND expressions using OR's
                    // (a) OR (b) OR (c) OR ...
                    BinaryExpression orExpression = ands.Aggregate(Expression.OrElse);

                    // Prepare an Expression<Func<BlogPost, bool>> that we can use in the .Where() method call
                    Type delegateFunc = typeof(Func<,>).MakeGenericType(typeof(BlogPost), typeof(bool));
                    Expression<Func<BlogPost, bool>> lambda = (Expression<Func<BlogPost, bool>>)Expression.Lambda(delegateFunc, orExpression, postArgument);

                    query = query.Where(lambda);
                }

                List<BlogPost> data = query.ToList();
                Console.WriteLine("Option 2:");
                Console.WriteLine(" Posts fetched: " + data.Count);
                Console.WriteLine(" Expression: " + query.Expression);
                Console.WriteLine(" SQL: " + query.ToQueryString());
                Console.WriteLine();
            }
        }

        private static BlogContext GetDatabase()
        {
            // Create a new context each time to ensure we don't pollute state.
            // This mimicks other code like ASP.Net websites. 
            // Additionally, we use an actual SQL-based database, to ensure that the created expression can be executed
            BlogContext context = new BlogContext(new DbContextOptionsBuilder<BlogContext>()
                .UseSqlite("Data Source=test.db")
                .Options);

            return context;
        }
    }

    class BlogContext : DbContext
    {
        public DbSet<BlogPost> Posts { get; set; }

        public BlogContext(DbContextOptions<BlogContext> options) : base(options)
        {
        }
    }

    class BlogPost
    {
        public int Id { get; set; }

        public string Author { get; set; }

        public string Title { get; set; }

        public string Content { get; set; }
    }
}
