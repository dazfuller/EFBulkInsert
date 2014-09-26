using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Transactions;
using System.Xml;
using System.Xml.Serialization;
using EFBulkInsert.Model;
using EntityFramework.BulkInsert.Extensions;

namespace EFBulkInsert
{
    public static class Program
    {
        private delegate void PerformPersistance(IList<Example> data);

        private const int CommitCount = 100;

        private static void Main()
        {
            var data = ReadDataFromInputFile(@"SampleData\\10K.xml");
            Console.WriteLine("Read {0:N0} lines", data.Count);

            RunDataPersistance(PersistDataClassic, data);
            RunDataPersistance(PersistDataClassicWithFrequentCommits, data);
            RunDataPersistance(PersistDataUsingFrequentCommits, data);
            RunDataPersistance(PersistDataUsingFrequentCommitsAndRecreateContext, data);
            RunDataPersistance(PersistDataUsingRange, data);
            RunDataPersistance(PersistDataUsingRangeAndFrequentCommits, data);
            RunDataPersistance(PersistDataUsingRangeFrequentCommitsAndRecreateContext, data);
            RunDataPersistance(PersistDataUsingBulkInsert, data);
            RunDataPersistance(PersistDataUsingSqlBulkCopy, data);
        }

        private static void RunDataPersistance(PerformPersistance persistance, IList<Example> data)
        {
            ClearDatabase();

            var sw = new Stopwatch();
            sw.Start();
            persistance(data);
            sw.Stop();

            var methodName = String.Format("{0}:", persistance.Method.Name);

            Console.WriteLine("{0,-60}{1}", methodName, sw.ElapsedMilliseconds);
        }

        private static void PersistDataUsingSqlBulkCopy(IEnumerable<Example> data)
        {
            using (var context = new ExampleContext())
            {
                const SqlBulkCopyOptions options = SqlBulkCopyOptions.CheckConstraints |
                                                   SqlBulkCopyOptions.KeepNulls |
                                                   SqlBulkCopyOptions.KeepIdentity;

                var inputData = new DataTable();
                
                var columns = context.GetColumns<Example>().ToList();
                
                foreach (var column in columns)
                {
                    inputData.Columns.Add(column.Item1, column.Item2);
                }

                foreach (var record in data)
                {
                    var row = inputData.NewRow();
                    foreach (var column in columns)
                    {
                        row[column.Item1] = typeof (Example).GetProperty(column.Item1).GetValue(record);
                    }
                    inputData.Rows.Add(row);
                }

                using (var tx = new TransactionScope())
                {
                    using (var bcp = new SqlBulkCopy(context.Database.Connection.ConnectionString, options))
                    {
                        bcp.BatchSize = 2500;
                        bcp.DestinationTableName = context.GetTableName<Example>();

                        bcp.WriteToServer(inputData);
                    }

                    tx.Complete();
                }
            }
        }

        private static void PersistDataClassic(IEnumerable<Example> data)
        {
            using (var context = new ExampleContext())
            {
                context.Configuration.AutoDetectChangesEnabled = false;

                foreach (var record in data)
                {
                    context.Example.Add(record);
                }

                context.SaveChanges();
            }
        }

        private static void PersistDataClassicWithFrequentCommits(IEnumerable<Example> data)
        {
            using (var context = new ExampleContext())
            {
                context.Configuration.AutoDetectChangesEnabled = false;

                foreach (var record in data)
                {
                    context.Example.Add(record);
                    if (record.Id == CommitCount)
                    {
                        context.SaveChanges();
                    }
                }

                context.SaveChanges();
            }
        }

        private static void PersistDataUsingRange(IEnumerable<Example> data)
        {
            using (var context = new ExampleContext())
            {
                context.Configuration.AutoDetectChangesEnabled = false;

                context.Set<Example>().AddRange(data);
                context.SaveChanges();
            }
        }

        private static void PersistDataUsingRangeAndFrequentCommits(IList<Example> data)
        {
            using (var context = new ExampleContext())
            {
                context.Configuration.AutoDetectChangesEnabled = false;

                var dataSize = data.Count;
                for (var i = 0; i < dataSize; i += CommitCount)
                {
                    context.Set<Example>().AddRange(data.Skip(i).Take(CommitCount));
                    context.SaveChanges();
                }
            }
        }

        private static void PersistDataUsingRangeFrequentCommitsAndRecreateContext(IList<Example> data)
        {
            ExampleContext context = null;

            try
            {
                context = new ExampleContext();
                context.Configuration.AutoDetectChangesEnabled = false;

                var dataSize = data.Count;
                for (var i = 0; i < dataSize; i += CommitCount)
                {
                    context.Set<Example>().AddRange(data.Skip(i).Take(CommitCount));
                    context.SaveChanges();

                    context.Dispose();
                    context = new ExampleContext();
                    context.Configuration.AutoDetectChangesEnabled = false;
                }
            }
            finally
            {
                if (context != null)
                {
                    context.Dispose();
                }
            }
        }

        private static void PersistDataUsingBulkInsert(IEnumerable<Example> data)
        {
            using (var context = new ExampleContext())
            {
                context.Configuration.AutoDetectChangesEnabled = false;

                using (var tx = new TransactionScope())
                {
                    context.BulkInsert(data, SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.CheckConstraints | SqlBulkCopyOptions.KeepNulls);
                    tx.Complete();
                }
            }
        }

        private static void PersistDataUsingFrequentCommits(IEnumerable<Example> data)
        {
            RunAddhocPersist(data, CommitCount, false);
        }

        private static void PersistDataUsingFrequentCommitsAndRecreateContext(IEnumerable<Example> data)
        {
            RunAddhocPersist(data, CommitCount, true);
        }

        private static void RunAddhocPersist(IEnumerable<Example> data, int commitCount, bool recreateContext)
        {
            ExampleContext context = null;

            try
            {
                context = new ExampleContext();
                context.Configuration.AutoDetectChangesEnabled = false;

                var count = 0;
                foreach (var record in data)
                {
                    count++;
                    context = AddToContext(context, record, count, commitCount, recreateContext);
                }
            }
            finally
            {
                if (context != null)
                {
                    context.Dispose();
                }
            }
        }

        private static ExampleContext AddToContext(ExampleContext context, Example record, int count, int commitCount, bool recreateContext)
        {
            context.Example.Add(record);

            if (count%commitCount == 0)
            {
                context.SaveChanges();
                if (recreateContext)
                {
                    context.Dispose();
                    context = new ExampleContext();
                    context.Configuration.AutoDetectChangesEnabled = false;
                }
            }

            return context;
        }

        private static void ClearDatabase()
        {
            using (var context = new ExampleContext())
            {
                var sql = String.Format("TRUNCATE TABLE {0}", context.GetTableName<Example>());
                context.Database.ExecuteSqlCommand(sql);
            }
        }

        private static List<Example> ReadDataFromInputFile(string inputFilePath)
        {
            using (var reader = new XmlTextReader(inputFilePath))
            {
                var serializer = new XmlSerializer(typeof(List<Example>));
                var data = (List<Example>)serializer.Deserialize(reader);
                return data;
            }
        }
    }
}
