﻿using Microsoft.ML;
using System.Data;

namespace ML
{
	internal class Program
	{
		static string _trainDataPath = Path.Combine(Environment.CurrentDirectory, "Data", "taxi-fare-train.csv");
		static string _testDataPath = Path.Combine(Environment.CurrentDirectory, "Data", "taxi-fare-test.csv");
		static string _modelPath = Path.Combine(Environment.CurrentDirectory, "Data", "Model.zip");

		static void Main(string[] args)
		{
			MLContext mlContext = new MLContext(seed: 0);
			var model = Train(mlContext, _trainDataPath);
			Evaluate(mlContext, model);
			TestSinglePrediction(mlContext, model);
		}

		static ITransformer Train(MLContext mlContext, string dataPath)
		{
			IDataView dataView = mlContext.Data.LoadFromTextFile<TaxiTrip>(dataPath, hasHeader: true, separatorChar: ',');
			var pipeline = mlContext.Transforms.CopyColumns(outputColumnName: "Label", inputColumnName: "FareAmount")
			.Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "VendorIdEncoded", inputColumnName: "VendorId"))
			.Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "RateCodeEncoded", inputColumnName: "RateCode"))
			.Append(mlContext.Transforms.Categorical.OneHotEncoding(outputColumnName: "PaymentTypeEncoded", inputColumnName: "PaymentType"))
			.Append(mlContext.Transforms.Concatenate("Features", "VendorIdEncoded", "RateCodeEncoded", "PassengerCount", "TripDistance", "PaymentTypeEncoded"))
			.Append(mlContext.Regression.Trainers.FastTree());
			var model = pipeline.Fit(dataView);
			return model;
		}
		static void Evaluate(MLContext mlContext, ITransformer model)
		{
			IDataView dataView = mlContext.Data.LoadFromTextFile<TaxiTrip>(_testDataPath, hasHeader: true, separatorChar: ',');
			var predictions = model.Transform(dataView);
			var metrics = mlContext.Regression.Evaluate(predictions, "Label", "Score");
			Console.WriteLine();
			Console.WriteLine($"*************************************************");
			Console.WriteLine($"*       Model quality metrics evaluation         ");
			Console.WriteLine($"*------------------------------------------------");
			Console.WriteLine($"*       RSquared Score:      {metrics.RSquared:0.##}");
			Console.WriteLine($"*       Root Mean Squared Error:      {metrics.RootMeanSquaredError:#.##}");
		}
		static void TestSinglePrediction(MLContext mlContext, ITransformer model)
		{
			var predictionFunction = mlContext.Model.CreatePredictionEngine<TaxiTrip, TaxiTripFarePrediction>(model);

			var taxiTripSample = new TaxiTrip()
			{
				VendorId = "VTS",
				RateCode = "1",
				PassengerCount = 1,
				TripTime = 1140,
				TripDistance = 3.75f,
				PaymentType = "CRD",
				FareAmount = 0 // To predict. Actual/Observed = 15.5
			};

			var prediction = predictionFunction.Predict(taxiTripSample);
			Console.WriteLine($"**********************************************************************");
			Console.WriteLine($"Predicted fare: {prediction.FareAmount:0.####}, actual fare: 15.5");
			Console.WriteLine($"**********************************************************************");
		}
	}
}
