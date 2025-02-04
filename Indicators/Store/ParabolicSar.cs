//==============================================================
// Forex Strategy Builder
// Copyright � Miroslav Popov. All rights reserved.
//==============================================================
// THIS CODE IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY KIND,
// EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO
// THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
// A PARTICULAR PURPOSE.
//==============================================================

using System;
using System.Drawing;
using ForexStrategyBuilder.Infrastructure.Entities;
using ForexStrategyBuilder.Infrastructure.Enums;
using ForexStrategyBuilder.Infrastructure.Interfaces;

namespace ForexStrategyBuilder.Indicators.Store
{
    public class ParabolicSar : Indicator
    {
        public ParabolicSar()
        {
            IndicatorName = "Parabolic SAR";
            PossibleSlots = SlotTypes.OpenFilter | SlotTypes.Close;

            IndicatorAuthor = "Miroslav Popov";
            IndicatorVersion = "2.0";
            IndicatorDescription = "Bundled in FSB distribution.";
        }

        public override void Initialize(SlotTypes slotType)
        {
            SlotType = slotType;

            // The ComboBox parameters
            IndParam.ListParam[0].Caption = "Logic";
            IndParam.IsAllowLTF = false;

            if (SlotType == SlotTypes.OpenFilter)
                IndParam.ListParam[0].ItemList = new[]
                {
                    "The price is higher than the PSAR value"
                };
            else if (SlotType == SlotTypes.Close)
                IndParam.ListParam[0].ItemList = new[]
                {
                    "Exit the market at PSAR"
                };
            else
                IndParam.ListParam[0].ItemList = new[]
                {
                    "Not Defined"
                };
            IndParam.ListParam[0].Index = 0;
            IndParam.ListParam[0].Text = IndParam.ListParam[0].ItemList[IndParam.ListParam[0].Index];
            IndParam.ListParam[0].Enabled = true;
            IndParam.ListParam[0].ToolTip = "Logic of application of the indicator.";

            // The NumericUpDown parameters
            IndParam.NumParam[0].Caption = "Starting AF";
            IndParam.NumParam[0].Value = 0.02;
            IndParam.NumParam[0].Min = 0.00;
            IndParam.NumParam[0].Max = 5.00;
            IndParam.NumParam[0].Point = 2;
            IndParam.NumParam[0].Enabled = true;
            IndParam.NumParam[0].ToolTip = "The starting value of Acceleration Factor.";

            IndParam.NumParam[1].Caption = "Increment";
            IndParam.NumParam[1].Value = 0.02;
            IndParam.NumParam[1].Min = 0.01;
            IndParam.NumParam[1].Max = 5.00;
            IndParam.NumParam[1].Point = 2;
            IndParam.NumParam[1].Enabled = true;
            IndParam.NumParam[1].ToolTip = "Increment value.";

            IndParam.NumParam[2].Caption = "Maximum AF";
            IndParam.NumParam[2].Value = 2.00;
            IndParam.NumParam[2].Min = 0.01;
            IndParam.NumParam[2].Max = 9.00;
            IndParam.NumParam[2].Point = 2;
            IndParam.NumParam[2].Enabled = true;
            IndParam.NumParam[2].ToolTip = "The maximum value of the Acceleration Factor.";
        }

        public override void Calculate(IDataSet dataSet)
        {
            DataSet = dataSet;

            var dAfMin = IndParam.NumParam[0].Value;
            var dAfInc = IndParam.NumParam[1].Value;
            var dAfMax = IndParam.NumParam[2].Value;

            // Reading the parameters
            double dPExtr;
            double dPsarNew = 0;
            var aiDir = new int[Bars];
            var adPsar = new double[Bars];

            //----	Calculating the initial values
            adPsar[0] = 0;
            var dAf = dAfMin;
            var intDirNew = 0;
            if (Close[1] > Open[0])
            {
                aiDir[0] = 1;
                aiDir[1] = 1;
                dPExtr = Math.Max(High[0], High[1]);
                adPsar[1] = Math.Min(Low[0], Low[1]);
            }
            else
            {
                aiDir[0] = -1;
                aiDir[1] = -1;
                dPExtr = Math.Min(Low[0], Low[1]);
                adPsar[1] = Math.Max(High[0], High[1]);
            }

            for (var bar = 2; bar < Bars; bar++)
            {
                //----	PSAR for the current period
                if (intDirNew != 0)
                {
                    // The direction was changed during the last period
                    aiDir[bar] = intDirNew;
                    intDirNew = 0;
                    adPsar[bar] = dPsarNew + dAf*(dPExtr - dPsarNew);
                }
                else
                {
                    aiDir[bar] = aiDir[bar - 1];
                    adPsar[bar] = adPsar[bar - 1] + dAf*(dPExtr - adPsar[bar - 1]);
                }

                // PSAR has to be out of the previous two bars limits
                if (aiDir[bar] > 0 && adPsar[bar] > Math.Min(Low[bar - 1], Low[bar - 2]))
                    adPsar[bar] = Math.Min(Low[bar - 1], Low[bar - 2]);
                else if (aiDir[bar] < 0 && adPsar[bar] < Math.Max(High[bar - 1], High[bar - 2]))
                    adPsar[bar] = Math.Max(High[bar - 1], High[bar - 2]);

                //----	PSAR for the next period

                // Calculation of the new values of flPExtr and flAF
                // if there is a new extreme price in the PSAR direction
                if (aiDir[bar] > 0 && High[bar] > dPExtr)
                {
                    dPExtr = High[bar];
                    dAf = Math.Min(dAf + dAfInc, dAfMax);
                }

                if (aiDir[bar] < 0 && Low[bar] < dPExtr)
                {
                    dPExtr = Low[bar];
                    dAf = Math.Min(dAf + dAfInc, dAfMax);
                }

                // Whether the price reaches PSAR
                if (Low[bar] <= adPsar[bar] && adPsar[bar] <= High[bar])
                {
                    intDirNew = -aiDir[bar];
                    dPsarNew = dPExtr;
                    dAf = dAfMin;
                    dPExtr = intDirNew > 0 ? High[bar] : Low[bar];
                }
            }
            const int firstBar = 8;

            // Saving the components
            Component = new IndicatorComp[1];

            Component[0] = new IndicatorComp
            {
                CompName = "PSAR value",
                DataType = SlotType == SlotTypes.Close ? IndComponentType.ClosePrice : IndComponentType.IndicatorValue,
                ChartType = IndChartType.Dot,
                ChartColor = Color.Violet,
                FirstBar = firstBar,
                PosPriceDependence = PositionPriceDependence.BuyHigherSellLower,
                Value = adPsar
            };
        }

        public override void SetDescription()
        {
            EntryFilterLongDescription = "the price is higher than the " + ToString();
            EntryFilterShortDescription = "the price is lower than the " + ToString();
            ExitPointLongDescription = "at " + ToString() + ". It determines the position direction also";
            ExitPointShortDescription = "at " + ToString() + ". It determines the position direction also";
        }

        public override string ToString()
        {
            return IndicatorName + " (" +
                   IndParam.NumParam[0].ValueToString + ", " + // Starting AF
                   IndParam.NumParam[1].ValueToString + ", " + // Increment
                   IndParam.NumParam[2].ValueToString + ")"; // Max AF
        }
    }
}