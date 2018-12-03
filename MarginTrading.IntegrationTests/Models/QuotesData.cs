using System;
using System.Collections.Generic;
using System.Linq;
using MarginTrading.IntegrationTests.Helpers;

namespace MarginTrading.IntegrationTests.Models
{
    public static class QuotesData
    {

        public static OrderBook GetHigherOrderBook() => new OrderBook(
            source: "FakeExchange", 
            assetPairId: SettingHelpers.GetInstrumentId,
            asks: new List<VolumePrice> {new VolumePrice(14, 100)}, 
            bids: new List<VolumePrice> {new VolumePrice(13, 100)},
            timestamp: DateTime.UtcNow);
        
        public static OrderBook GetNormalOrderBook() => new OrderBook(
            source: "FakeExchange", 
            assetPairId: SettingHelpers.GetInstrumentId,
            asks: new List<VolumePrice> {new VolumePrice(11, 100)}, 
            bids: new List<VolumePrice> {new VolumePrice(10, 100)},
            timestamp: DateTime.UtcNow);

        public static OrderBook GetLowerOrderBook() => new OrderBook(
            source: "FakeExchange", 
            assetPairId: SettingHelpers.GetInstrumentId,
            asks: new List<VolumePrice> {new VolumePrice(8, 100)}, 
            bids: new List<VolumePrice> {new VolumePrice(7, 100)},
            timestamp: DateTime.UtcNow);

        public static OrderBook GetMuchLowerOrderBook() => new OrderBook(
            source: "FakeExchange", 
            assetPairId: SettingHelpers.GetInstrumentId,
            asks: new List<VolumePrice> {new VolumePrice(5, 100)}, 
            bids: new List<VolumePrice> {new VolumePrice(4, 100)},
            timestamp: DateTime.UtcNow);
    }
}
