using System;
using System.Collections.Generic;
using System.Linq;

namespace MarginTrading.IntegrationTests.Models
{
    public static class QuotesData
    {
        public static OrderBook GetNormalOrderBook() => new OrderBook(
            source: "FakeExchange", 
            assetPairId: MtSettingsData.TradingInstruments().First().Instrument,
            asks: new List<VolumePrice> {new VolumePrice(11, 100)}, 
            bids: new List<VolumePrice> {new VolumePrice(10, 100)},
            timestamp: DateTime.UtcNow);

        public static OrderBook GetLowerOrderBook() => new OrderBook(
            source: "FakeExchange", 
            assetPairId: MtSettingsData.TradingInstruments().First().Instrument,
            asks: new List<VolumePrice> {new VolumePrice(8, 100)}, 
            bids: new List<VolumePrice> {new VolumePrice(7, 100)},
            timestamp: DateTime.UtcNow);
    }
}
