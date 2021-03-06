﻿using PLplot;
using System;
using System.Reflection;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage;

namespace SineWaves
{
    internal static class Program
    {
        public class StartStopVMEntry : TableEntity
        {
            public StartStopVMEntry()
            { }

            public StartStopVMEntry(string pkey, string datetime)
            {
                this.PartitionKey = pkey;
                this.RowKey = datetime;
            }

            public string VMName { get; set; }

            public string VMType { get; set; }

            public string VMStartedOrStopped { get; set; }

            public DateTime VMdatetime { get; set; }
        }

        public async static void Storage()
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse("UseDevelopmentStorage=true");
            CloudTableClient cloudTableClient = storageAccount.CreateCloudTableClient();
            CloudTable table = cloudTableClient.GetTableReference("StartStopVMs");
            await table.CreateIfNotExistsAsync();

            /*
            for (int i = 0; i < 12; i++)
            {
                DateTime x = DateTime.Now.Subtract(TimeSpan.FromHours(i*6));
                string partitionKey = "partition";
                var entry = new StartStopVMEntry(partitionKey, String.Format("{0:d21}{1}{2}", DateTimeOffset.MaxValue.UtcDateTime.Ticks - new DateTimeOffset(x).UtcDateTime.Ticks, "-", Guid.NewGuid().ToString()));
                entry.VMName = "vm";
                entry.VMType = "b";
                entry.VMStartedOrStopped = "c";
                entry.VMdatetime = x;
                await table.ExecuteAsync(TableOperation.Insert(entry));
            }
            */

            //Find all entries from now to before 

            var fromTime = DateTime.UtcNow;
            var fromTicks = String.Format("{0:d21}", DateTimeOffset.MaxValue.UtcDateTime.Ticks - new DateTimeOffset(fromTime).UtcDateTime.Ticks);

            var toTime = DateTime.UtcNow.Subtract(TimeSpan.FromDays(30));
            var toTicks = String.Format("{0:d21}", DateTimeOffset.MaxValue.UtcDateTime.Ticks - new DateTimeOffset(toTime).UtcDateTime.Ticks);

            string pkFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, "partition");
            string rkLowerFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.GreaterThanOrEqual, fromTicks);
            string rkUpperFilter = TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.LessThan, toTicks);

            // Note CombineFilters has the effect of “([Expression1]) Operator (Expression2]), as such passing in a complex expression will result in a logical grouping. 
            string combinedRowKeyFilter = TableQuery.CombineFilters(rkLowerFilter, TableOperators.And, rkUpperFilter);
            string combinedFilter = TableQuery.CombineFilters(pkFilter, TableOperators.And, combinedRowKeyFilter);
            TableQuery<StartStopVMEntry> query = new TableQuery<StartStopVMEntry>().Where(combinedFilter);

            TableContinuationToken token = null;
            query.TakeCount = 50;
            int segmentNumber = 0;
            do
            {
                // Execute the query, passing in the continuation token.
                // The first time this method is called, the continuation token is null. If there are more results, the call
                // populates the continuation token for use in the next call.
                TableQuerySegment<StartStopVMEntry> segment = await table.ExecuteQuerySegmentedAsync(query, token);

                // Indicate which segment is being displayed
                if (segment.Results.Count > 0)
                {
                    segmentNumber++;
                    Console.WriteLine();
                    Console.WriteLine("Segment {0}", segmentNumber);
                }

                // Save the continuation token for the next call to ExecuteQuerySegmentedAsync
                token = segment.ContinuationToken;

                // Write out the properties for each entity returned.
                foreach (StartStopVMEntry entity in segment)
                {
                    Console.WriteLine("\t Customer: {0},{1},{2},{3}", entity.PartitionKey, entity.RowKey, entity.VMdatetime.ToShortDateString(), entity.Timestamp);
                }

                Console.WriteLine();
            }
            while (token != null);
        }

        public static void Main(string[] args)
        {
            Storage();


            // generate data for plotting
            const double sineFactor = 0.012585;
            const int exampleCount = 1000;
            var phaseOffset = 125;

            var x0 = new double[exampleCount];
            var y0 = new double[exampleCount];

            for (var j = 0; j < exampleCount; j++)
            {
                x0[j] = j;
                y0[j] = (double)(System.Math.Sin(sineFactor * (j + phaseOffset)) * 1);
            }

            var x1 = new double[exampleCount];
            var y1 = new double[exampleCount];
            phaseOffset = 250;

            for (var j = 0; j < exampleCount; j++)
            {
                x1[j] = j;
                y1[j] = (double)(System.Math.Sin(sineFactor * (j + phaseOffset)) * 1);
            }

            var x2 = new double[exampleCount];
            var y2 = new double[exampleCount];
            phaseOffset = 375;

            for (var j = 0; j < exampleCount; j++)
            {
                x2[j] = j;
                y2[j] = (double)(System.Math.Sin(sineFactor * (j + phaseOffset)) * 1);
            }

            var x3 = new double[exampleCount];
            var y3 = new double[exampleCount];
            phaseOffset = 500;

            for (var j = 0; j < exampleCount; j++)
            {
                x3[j] = j;
                y3[j] = (double)(System.Math.Sin(sineFactor * (j + phaseOffset)) * 1);
            }

            // create PLplot object
            var pl = new PLStream();

            // use SVG backend and write to SineWaves.svg in current directory
            if (args.Length == 1 && args[0] == "svg")
            {
                pl.sdev("svg");
                pl.sfnam("SineWaves.svg");
            }
            else
            {
                pl.sdev("pngcairo");
                pl.sfnam("SineWaves.png");
            }

            // use white background with black foreground
            pl.spal0("cmap0_alternate.pal");

            // Initialize plplot
            pl.init();

            // set axis limits
            const int xMin = 0;
            const int xMax = 1000;
            const int yMin = -1;
            const int yMax = 1;
            pl.env(xMin, xMax, yMin, yMax, AxesScale.Independent, AxisBox.BoxTicksLabelsAxes);

            // Set scaling for mail title text 125% size of default
            pl.schr(0, 1.25);

            // The main title
            pl.lab("X", "Y", "PLplot demo of four sine waves");

            // plot using different colors
            // see http://plplot.sourceforge.net/examples.php?demo=02 for palette indices
            pl.col0(9);
            pl.line(x0, y0);
            pl.col0(1);
            pl.line(x1, y1);
            pl.col0(2);
            pl.line(x2, y2);
            pl.col0(4);
            pl.line(x3, y3);

            // end page (writes output to disk)
            pl.eop();

            // output version
            pl.gver(out var verText);
            Console.WriteLine("PLplot version " + verText);

            ////////histogram/////////////////////
            

             double[] y00 =  { 5.0, 15.0, 12.0, 24.0, 28.0, 30.0, 20.0, 8.0, 12.0, 3.0 };

             double[] pos = { 0.0, 0.25, 0.5, 0.75, 1.0 };
             double[] red = { 0.0, 0.25, 0.5, 1.0, 1.0 };
             double[] green = { 1.0, 0.5, 0.5, 0.5, 1.0 };
             double[] blue = { 1.0, 1.0, 0.5, 0.25, 0.0 };

            PLStream pls = new PLStream();
            pls.sdev("pngcairo");
            pls.sfnam("Histogram.png");
            pls.spal0("cmap0_alternate.pal");
            pls.init();
            pls.adv(0);
            pls.vsta();
            pls.wind(1980.0, 1990.0, 0.0, 35.0);
            pls.box("bc", 1.0, 0, "bcnv", 10.0, 0);
            pls.col0(2);
            pls.lab("Year", "Widget Sales (millions)", "#frPLplot Example 12");
            for (int i = 0; i < 10; i++)
            {
                //            pls.col0(i + 1);
                pls.col1(i / 9.0);
                pls.psty(0);                

                double[] x = new double[4];
                double[] y = new double[4];

                x[0] = 1980.0 + i;
                y[0] = 0.0;
                x[1] = 1980.0 + i;
                y[1] = y00[i];
                x[2] = 1980.0 + i + 1.0;
                y[2] = y00[i];
                x[3] = 1980.0 + i + 1.0;
                y[3] = 0.0;
                pls.fill(x, y);
                pls.col0(1);
                pls.lsty(LineStyle.Continuous);
                pls.line(x, y);
                

                //	   sprintf(string, "%.0f", y0[i]);
                String text = ((int)(y00[i] + 0.5)).ToString();
                pls.ptex((1980.0 + i + .5), (y00[i] + 1.0 ), 1.0, 0.0, .5, text);
                //	   sprintf(string, "%d", 1980 + i);
                String text1 =  (1980 + i).ToString();
                pls.mtex("b", 1.0, ((i + 1) * .1 - .05), 0.5, text1);
            }
            pls.eop();
            Console.ReadLine();
        } 
    }
}