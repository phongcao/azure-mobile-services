﻿using Microsoft.WindowsAzure.MobileServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using ZumoE2ETestApp.Framework;
using ZumoE2ETestApp.Tests.Types;

namespace ZumoE2ETestApp.Tests
{
    internal static class ZumoQueryTests
    {
        internal static ZumoTestGroup CreateTests()
        {
            ZumoTestGroup result = new ZumoTestGroup("Query tests");
            result.AddTest(new ZumoTest("Populate table, if necessary", new TestExecution(async delegate(ZumoTest test)
            {
                var client = ZumoTestGlobals.Instance.Client;
                var table = client.GetTable<AllMovies>();
                AllMovies allMovies = new AllMovies
                {
                    Movies = ZumoQueryTestData.AllMovies
                };
                await table.InsertAsync(allMovies);
                test.AddLog("Result of populating table: {0}", allMovies.Status);
                return true;
            })));

            // Numeric fields
            result.AddTest(CreateQueryTest("GreaterThan and LessThan - Movies from the 90s", m => m.Year > 1989 && m.Year < 2000));
            result.AddTest(CreateQueryTest("GreaterEqual and LessEqual - Movies from the 90s", m => m.Year >= 1990 && m.Year <= 1999));
            result.AddTest(CreateQueryTest("Compound statement - OR of ANDs - Movies from the 30s and 50s",
                m => ((m.Year >= 1930) && (m.Year < 1940)) || ((m.Year >= 1950) && (m.Year < 1960))));
            result.AddTest(CreateQueryTest("Division, equal and different - Movies from the year 2000 with rating other than R",
                m => ((m.Year / 1000.0) == 2) && (m.Rating != "R")));
            result.AddTest(CreateQueryTest("Addition, subtraction, relational, AND - Movies from the 1980s which last less than 2 hours",
                m => ((m.Year - 1900) >= 80) && (m.Year + 10 < 2000) && (m.Duration < 120)));

            // String functions
            result.AddTest(CreateQueryTest("StartsWith - Movies which starts with 'The'",
                m => m.Title.StartsWith("The"), 100));
            result.AddTest(CreateQueryTest("StartsWith, case insensitive - Movies which start with 'the'",
                m => m.Title.ToLower().StartsWith("the"), 100));
            result.AddTest(CreateQueryTest("EndsWith, case insensitive - Movies which end with 'r'",
                m => m.Title.ToLower().EndsWith("r")));
            result.AddTest(CreateQueryTest("Contains - Movies which contain the word 'one', case insensitive",
                m => m.Title.ToUpper().Contains("ONE")));

            // TODO: Add more string functions

            // Numeric functions
            result.AddTest(CreateQueryTest("Floor - Movies which last more than 3 hours",
                m => Math.Floor(m.Duration / 60.0) >= 3));
            result.AddTest(CreateQueryTest("Ceiling - Best picture winners which last at most 2 hours",
                m => m.BestPictureWinner == true && Math.Ceiling(m.Duration / 60.0) == 2));
            result.AddTest(CreateQueryTest("Round - Best picture winners which last more than 2.5 hours",
                m => m.BestPictureWinner == true && Math.Round(m.Duration / 60.0) > 2));

            // Date fields
            result.AddTest(CreateQueryTest("Date: Greater than, less than - Movies with release date in the 70s",
                m => m.ReleaseDate > new DateTime(1969,12,31,0,0,0,DateTimeKind.Utc) && 
                    m.ReleaseDate < new DateTime(1971, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
            result.AddTest(CreateQueryTest("Date: Greater than, less than - Movies with release date in the 80s",
                m => m.ReleaseDate >= new DateTime(1980,1,1,0,0,0,DateTimeKind.Utc) && 
                    m.ReleaseDate < new DateTime(1989, 12, 31, 23, 59, 59, DateTimeKind.Utc)));
            result.AddTest(CreateQueryTest("Date: Equal - Movies released on 1994-10-14 (Shawshank Redemption / Pulp Fiction)",
                m => m.ReleaseDate == new DateTime(1994, 10, 14, 0, 0, 0, DateTimeKind.Utc)));
            
            // Date functions
            result.AddTest(CreateQueryTest("Date (month): Movies released in November",
                m => m.ReleaseDate.Month == 11));
            result.AddTest(CreateQueryTest("Date (day): Movies released in the first day of the month",
                m => m.ReleaseDate.Day == 1));
            result.AddTest(CreateQueryTest("Date (year): Movies whose year is different than its release year",
                m => m.ReleaseDate.Year != m.Year, 100));

            // Bool fields
            result.AddTest(CreateQueryTest("Bool: equal to true - Best picture winners before 1950",
                m => m.Year < 1950 && m.BestPictureWinner == true));
            result.AddTest(CreateQueryTest("Bool: equal to false - Best picture winners after 2000",
                m => m.Year >= 2000 && !(m.BestPictureWinner == false)));
            result.AddTest(CreateQueryTest("Bool: not equal to false - Best picture winners after 2000",
                m => m.BestPictureWinner != false && m.Year >= 2000));
    
            // Top and skip
            result.AddTest(CreateQueryTest("Get all using large $top - 500", null, 500));
            result.AddTest(CreateQueryTest("Skip all using large skip - 500", null, null, 500));
            result.AddTest(CreateQueryTest("Skip, take, includeTotalCount - movies 11-20, ordered by title",
                null, 10, 10, new[] { new OrderByClause("Title", true) }, null, true));
            result.AddTest(CreateQueryTest("Skip, take, filter includeTotalCount - movies 11-20 which won a best picture award, ordered by year",
                m => m.BestPictureWinner == true, 10, 10, new[] { new OrderByClause("Year", false) }, null, true));

            // Order by
            result.AddTest(CreateQueryTest("Order by date and string - 50 movies, ordered by release date, then title",
                null, 50, null, new[] { new OrderByClause("ReleaseDate", false), new OrderByClause("Title", true) }));
            result.AddTest(CreateQueryTest("Order by number - 30 shortest movies since 1970",
                m => m.Year >= 1970, 30, null, new[] { new OrderByClause("Duration", true), new OrderByClause("Title", true) }, null, true));

            // Negative tests
            result.AddTest(CreateQueryTest<MobileServiceInvalidOperationException>("(Neg) Very large top value", m => m.Year > 2000, 1001));
            result.AddTest(CreateQueryTest<ArgumentException>("(Neg) Unsupported predicate: unsupported arithmetic",
                m => Math.Sqrt(m.Year) > 43));
            
            // Invalid lookup
            for (int i = -1; i <= 0; i++)
            {
                int id = i;
                result.AddTest(new ZumoTest("(Neg) Invalid id for lookup: " + i, async delegate(ZumoTest test)
                {
                    var table = ZumoTestGlobals.Instance.Client.GetTable<Movie>();
                    try
                    {
                        var item = await table.LookupAsync(id);
                        test.AddLog("Error, LookupAsync for id = {0} should have failed, but succeeded: {1}", id, item);
                        return false;
                    }
                    catch (ArgumentException ex)
                    {
                        test.AddLog("Caught expected exception - {0}: {1}", ex.GetType().FullName, ex.Message);
                        return true;
                    }
                }));
            }

            return result;
        }

        class OrderByClause
        {
            public OrderByClause(string fieldName, bool isAscending)
            {
                this.FieldName = fieldName;
                this.IsAscending = isAscending;
            }

            public bool IsAscending { get; private set; }
            public string FieldName { get; private set; }

            public Expression<Func<Movie, TKey>> GetKeySelector<TKey>()
            {
                string fieldName = this.FieldName.ToLowerInvariant();
                if (typeof(TKey) == typeof(string))
                {
                    if (fieldName == "title")
                    {
                        return m => (TKey)(object)m.Title;
                    }
                    else if (fieldName == "rating")
                    {
                        return m => (TKey)(object)m.Rating;
                    }
                }
                else if (typeof(TKey) == typeof(int))
                {
                    if (fieldName == "year")
                    {
                        return m => (TKey)(object)m.Year;
                    }
                    else if (fieldName == "duration")
                    {
                        return m => (TKey)(object)m.Duration;
                    }
                }
                else if (typeof(TKey) == typeof(DateTime))
                {
                    if (fieldName == "releasedate")
                    {
                        return m => (TKey)(object)m.ReleaseDate;
                    }
                }

                throw new InvalidOperationException(string.Format("Cannot create a selector for field {0} with type {1}.", FieldName, typeof(TKey)));
            }
        }

        private static ZumoTest CreateQueryTest(
            string name, Expression<Func<Movie, bool>> whereClause,
            int? top = null, int? skip = null, OrderByClause[] orderBy = null,
            string[] selectFields = null, bool? includeTotalCount = null)
        {
            return CreateQueryTest<ExceptionTypeWhichWillNeverBeThrown>(name, whereClause, top, skip, orderBy, selectFields, includeTotalCount);
        }

        private static ZumoTest CreateQueryTest<TExpectedException>(
            string name, Expression<Func<Movie, bool>> whereClause, 
            int? top = null, int? skip = null, OrderByClause[] orderBy = null,
            string[] selectFields = null, bool? includeTotalCount = null)
            where TExpectedException : Exception
        {
            return new ZumoTest(name, async delegate(ZumoTest test)
            {
                try
                {
                    var table = ZumoTestGlobals.Instance.Client.GetTable<Movie>();
                    MobileServiceTableQuery<Movie> query = null;

                    if (whereClause != null)
                    {
                        query = table.Where(whereClause);
                    }

                    if (orderBy != null)
                    {
                        if (query == null)
                        {
                            query = table.Where(m => m.Duration == m.Duration);
                        }

                        query = ApplyOrdering(query, orderBy);
                    }

                    if (top.HasValue)
                    {
                        query = query == null ? table.Take(top.Value) : query.Take(top.Value);
                    }

                    if (skip.HasValue)
                    {
                        query = query == null ? table.Skip(skip.Value) : query.Skip(skip.Value);
                    }

                    if (selectFields != null)
                    {
                        throw new ArgumentException("Select tests not implemented yet");
                    }

                    if (includeTotalCount.HasValue)
                    {
                        query = query.IncludeTotalCount();
                    }

                    var readData = await query.ToEnumerableAsync();
                    long actualTotalCount = -1;
                    ITotalCountProvider totalCountProvider = readData as ITotalCountProvider;
                    if (totalCountProvider != null)
                    {
                        actualTotalCount = totalCountProvider.TotalCount;
                    }

                    var data = readData.ToArray();
                    IEnumerable<Movie> expectedData = ZumoQueryTestData.AllMovies;
                    if (whereClause != null)
                    {
                        expectedData = expectedData.Where(whereClause.Compile());
                    }

                    long expectedTotalCount = -1;
                    if (includeTotalCount.HasValue && includeTotalCount.Value)
                    {
                        expectedTotalCount = expectedData.Count();
                    }

                    if (orderBy != null)
                    {
                        expectedData = ApplyOrdering(expectedData, orderBy);
                    }

                    if (skip.HasValue)
                    {
                        expectedData = expectedData.Skip(skip.Value);
                    }

                    if (top.HasValue)
                    {
                        expectedData = expectedData.Take(top.Value);
                    }

                    List<string> errors = new List<string>();

                    if (includeTotalCount.HasValue)
                    {
                        if (expectedTotalCount != actualTotalCount)
                        {
                            test.AddLog("Total count was requested, but the returned value is incorrect: expected={0}, actual={1}", expectedTotalCount, actualTotalCount);
                            return false;
                        }
                    }

                    if (!Util.CompareArrays(expectedData.ToArray(), data, errors))
                    {
                        foreach (var error in errors)
                        {
                            test.AddLog(error);
                        }

                        test.AddLog("Expected data is different");
                        return false;
                    }
                    else
                    {
                        if (typeof(TExpectedException) == typeof(ExceptionTypeWhichWillNeverBeThrown))
                        {
                            return true;
                        }
                        else
                        {
                            test.AddLog("Error, test should have failed with {0}, but succeeded.", typeof(TExpectedException).FullName);
                            return false;
                        }
                    }
                }
                catch (TExpectedException ex)
                {
                    test.AddLog("Caught expected exception - {0}: {1}", ex.GetType().FullName, ex.Message);
                    return true;
                }
            });
        }

        private static MobileServiceTableQuery<Movie> ApplyOrdering(MobileServiceTableQuery<Movie> query, OrderByClause[] orderBy)
        {
            if (orderBy.Length == 1)
            {
                if (orderBy[0].IsAscending && orderBy[0].FieldName == "Title")
                {
                    return query.OrderBy(m => m.Title);
                }
                else if (!orderBy[0].IsAscending && orderBy[0].FieldName == "Year")
                {
                    return query.OrderByDescending(m => m.Year);
                }
            }
            else if (orderBy.Length == 2)
            {
                if (orderBy[1].FieldName == "Title" && orderBy[1].IsAscending)
                {
                    if (orderBy[0].FieldName == "Duration" && orderBy[0].IsAscending)
                    {
                        return query.OrderBy(m => m.Duration).ThenBy(m => m.Title);
                    }
                    else if (orderBy[0].FieldName == "ReleaseDate" && !orderBy[0].IsAscending)
                    {
                        return query.OrderByDescending(m => m.ReleaseDate).ThenBy(m => m.Title);
                    }
                }
            }

            throw new NotImplementedException(string.Format("Ordering by [{0}] not implemented yet",
                string.Join(", ", orderBy.Select(c => string.Format("{0} {1}", c.FieldName, c.IsAscending ? "asc" : "desc")))));
        }

        private static IEnumerable<Movie> ApplyOrdering(IEnumerable<Movie> data, OrderByClause[] orderBy)
        {
            if (orderBy.Length == 1)
            {
                if (orderBy[0].IsAscending && orderBy[0].FieldName == "Title")
                {
                    return data.OrderBy(m => m.Title);
                }
                else if (!orderBy[0].IsAscending && orderBy[0].FieldName == "Year")
                {
                    return data.OrderByDescending(m => m.Year);
                }
            }
            else if (orderBy.Length == 2)
            {
                if (orderBy[1].FieldName == "Title" && orderBy[1].IsAscending)
                {
                    if (orderBy[0].FieldName == "Duration" && orderBy[0].IsAscending)
                    {
                        return data.OrderBy(m => m.Duration).ThenBy(m => m.Title);
                    }
                    else if (orderBy[0].FieldName == "ReleaseDate" && !orderBy[0].IsAscending)
                    {
                        return data.OrderByDescending(m => m.ReleaseDate).ThenBy(m => m.Title);
                    }
                }
            }

            throw new NotImplementedException(string.Format("Ordering by [{0}] not implemented yet",
                string.Join(", ", orderBy.Select(c => string.Format("{0} {1}", c.FieldName, c.IsAscending ? "asc" : "desc")))));
        }
    }
}
