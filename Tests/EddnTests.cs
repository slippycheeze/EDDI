﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.IO.Compression;
using NetMQ;
using NetMQ.Sockets;

namespace Tests
{
    [TestClass]
    public class EddnTests
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")] // this usage is perfectly correct
        [TestMethod]
        public void TestListen()
        {
            using (var subscriber = new SubscriberSocket())
            {
                int i = 0;
                subscriber.Connect("tcp://eddn.edcd.io:9500");
                subscriber.Subscribe("");
                while (i < 10)
                {
                    string data;
                    byte[] compressed = subscriber.ReceiveFrameBytes();
                    using (var stream = new MemoryStream(compressed, 2, compressed.Length - 2))
                    using (var inflater = new DeflateStream(stream, CompressionMode.Decompress))
                    using (var streamReader = new StreamReader(inflater))
                    {
                        data = streamReader.ReadToEnd();
                    }
                    Console.WriteLine(data);
                    i++;
                }
            }
        }

        [TestMethod()]
        public void TestEventSystemNameMatches()
        {
            EDDNResponder.EDDNResponder responder = new EDDNResponder.EDDNResponder();
            var privateObject = new PrivateObject(responder);

            // The EDDN responder tracks system names and coordinates independently. 
            // Intentionally place our EDDN responder in a state with no coordinates available.
            privateObject.SetProperty("systemName", "Not in this galaxy");
            privateObject.SetProperty("systemX", null);
            privateObject.SetProperty("systemY", null);
            privateObject.SetProperty("systemZ", null);

            // Force a call to the method
            responder.eventSystemNameMatches("Artemis");

            // Test that the results, including coordinates, have been correctly retrieved by the EDDN responder
            Assert.AreEqual("Artemis", responder.systemName);
            Assert.AreEqual(14.28125, (double)responder.systemX, 0.00001);
            Assert.AreEqual(-63.1875, (double)responder.systemY, 0.00001);
            Assert.AreEqual(-24.875, (double)responder.systemZ, 0.00001);
        }
    }
}
