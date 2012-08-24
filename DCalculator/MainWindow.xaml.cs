using System;
using System.Windows;
using System.Windows.Controls;
using D_Parser.Misc;
using D_Parser.Parser;
using D_Parser.Resolver;
using D_Parser.Resolver.ExpressionSemantics;

namespace DCalculator
{
	/// <summary>
	/// Interaktionslogik für MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		ResolverContextStack ctxt = new ResolverContextStack(ParseCacheList.Create(), new ResolverContext());

		public MainWindow()
		{
			InitializeComponent();

			text_input.Text = "2+4";
		}

		public void Calc()
		{
			try
			{
				var e = DParser.ParseExpression(text_input.Text);
				var v = Evaluation.EvaluateValue(e, ctxt);

				text_result.Text = v==null ? "[No result]" : v.ToCode();
			}
			catch (EvaluationException ee)
			{
				text_result.Text = "[\""+ee.EvaluatedExpression.ToString() + "\": "+ee.Message+"]";
			}
			catch (Exception ex)
			{
				text_result.Text = "[Expression parsing error | " + ex.Message + "]";
			}
		}

		private void button1_Click(object sender, RoutedEventArgs e)
		{
			Calc();
		}

		private void text_input_TextChanged(object sender, TextChangedEventArgs e)
		{
			Calc();
		}
	}
}
