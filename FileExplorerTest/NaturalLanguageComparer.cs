using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FileExplorerTest
{
	/// <summary>Comparer to be used when comparing file system names.  Will not be the same as Explorer, but will group things together logically.</summary>
	public class NaturalLanguageComparer : IComparer<string>
	{
		// RULES for Natural Language Comparer:
		// * Strings must have no leading or trailing spaces (as this sort is mainly used for sorting folder items).
		// * Alphanumeric comparisons are case insensitive (e.g. 'a' == 'A')
		// * The character '/' is a special character and should not be present (it's not a valid char in folder item names).
		// * A name split into smaller groups by whitespace.
		//   * Consecutive whitespace is treated as one group divider, but the number of characters is significant.
		//   * The number of whitespace characters is stored for comparison
		// * Groups are further split depending on whether they contain a number or not.
		//   * If no number is present, the Prefix contains the whole group text
		//   * If a number is present:
		//     * the Prefix contains the text before the number (if any)
		//     * the Suffix contains the text after the number (if any)
		// * A number is a positive whole or decimal number.
		//   * Negative numbers are not possible.  The '-' is considered to not be part of a number.
		// * A number does not include a '+' or  '-' sign at the front.
		// * Number separators and Decimal points are subject to the Windows culture settings
		// * A number can containe any number separators in any position within the number
		//   * A number separator cannot be at the start or end of the number; this is considered to be text
		//   * When a number separator is before a decimal point in a decimal number, it terminates the number
		//     (making it a whole number) and the remaining characters are considered text (even if those characters are digits).
		//   * When a number separator is after a decimal point in a decimal number, it terminates the number
		//     (making it a whole number).  The decimal becomes part of the whole number and the remaining characters (including
		//     group separator) are considered text (even if those characters are digits).
		// * A number can contain zero or one decimal point
		//   * If more than one decimal point is found, it and any following non-whitespace characters are considered text
		//     (even if those characters are digits).

		Regex RegexReplaceSpaces = new Regex(@"\s+", RegexOptions.Compiled);
		Regex RegexMatchDigit = new Regex(@"\d", RegexOptions.Compiled);

		/// <summary>Compares folder item names to gain results as close to Windows 10 File Explorer as possible</summary>
		public int Compare(string a, string b)
		{
			a = a?.Trim()?.ToLower();
			if (a.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(a));

			b = b?.Trim()?.ToLower();
			if (b.IsNullOrWhiteSpace()) throw new ArgumentNullException(nameof(b));

			// Quick comparison
			if (a.CompareTo(b) == 0)
				return 0;

			var aProcessor = new GroupProcessor(a);
			var bProcessor = new GroupProcessor(b);

			do // process the current groups
			{

				/// Prefix comparison


				var aHasPrefix = aProcessor.Prefix.Any();
				var bHasPrefix = bProcessor.Prefix.Any();

				if (aHasPrefix && !bHasPrefix)                                  // b must have a number with no prefix
					return aProcessor.Prefix.CompareTo(bProcessor.TextNumber);  // use alphanumeric comparison to number in b as text

				else if (!aHasPrefix && bHasPrefix)                             // a must have a number with no prefix
					return aProcessor.TextNumber.CompareTo(bProcessor.Prefix);  // use alphanumeric comparison to number in a as text

				else if (aHasPrefix && bHasPrefix)  // both a and b have a prefix
				{
					var prefixComparison = aProcessor.Prefix.CompareTo(bProcessor.Prefix);
					if (prefixComparison != 0)
						return prefixComparison;
				}


				/// Number comparison


				// Both a and have numbers or both a and be do not have numbers at this point.

				if (aProcessor.IsNumberAvailable && bProcessor.IsNumberAvailable)
				{

					if (aProcessor.IsWholeNumberPartProvided && !bProcessor.IsWholeNumberPartProvided)
						return aProcessor.WholeNumber == 0
							? -1    // a < b : b has a bigger number
							: 1;    // a > b : a has a bigger number

					else if (!aProcessor.IsWholeNumberPartProvided && bProcessor.IsWholeNumberPartProvided)
						return bProcessor.WholeNumber == 0
							? 1     // a > b : a has a bigger number
							: -1;   // a < b : b has a bigger number

					else if (aProcessor.IsWholeNumberPartProvided && bProcessor.IsWholeNumberPartProvided)
					{
						var wholeNumberComparison = aProcessor.WholeNumber.CompareTo(bProcessor.WholeNumber);
						if (wholeNumberComparison != 0)
							return wholeNumberComparison;
					}


					// Both a and b have equal or no whole number parts at this point.


					// Check decimal parts
					if (aProcessor.IsDecimalNumberPartProvided && !bProcessor.IsDecimalNumberPartProvided)
						return 1;   // a > b : a has a bigger number
					else if (!aProcessor.IsDecimalNumberPartProvided && bProcessor.IsDecimalNumberPartProvided)
						return -1;  // a < b : b has a bigger number
					else if (aProcessor.IsDecimalNumberPartProvided && bProcessor.IsDecimalNumberPartProvided)
					{

						// Both a and b have decimal parts at this point.


						if (aProcessor.DecimalNumberText.CompareTo(bProcessor.DecimalNumberText) != 0)
						{
							// Not textually the same.  Could still be numerically the same with number grouping chars in different places.

							var aParsed = double.TryParse(aProcessor.DecimalNumberText, out var aDecimalPart);
							var bParsed = double.TryParse(bProcessor.DecimalNumberText, out var bDecimalPart);

							// NOTE: Both a and b decimal numbers should parse to text.
							// We use a defensive programming check here anyway to make extra sure.
							if (aParsed && bParsed)
							{
								// NOTE: Very long numbers will be truncated.  
								// We could mix this with text comparison, but the value in doing that is minimal so we ignore it.
								var decimalComparison = aDecimalPart.CompareTo(bDecimalPart);
								if (decimalComparison != 0)
									return decimalComparison;
							}
							else    // Defensive check failed!
							{
								if (!aParsed)
								{ }     // TODO: Logger.Write
								if (!bParsed)
								{ }     // TODO: Logger

								var textComparison = aProcessor.DecimalNumberText.CompareTo(bProcessor.DecimalNumberText);
								if (textComparison != 0)
									return textComparison;
							}
						}
					}


					// Both a and b have numbers with equal or no decimal parts at this point.


					/// Suffix comparison


					var suffixComparison = aProcessor.Suffix.CompareTo(bProcessor.Suffix);
					if (suffixComparison != 0)
						return suffixComparison;
				}


				// Both a and b have equal numbers and suffixes, or no numbers, at this point.


				/// Group count comparison


				if (aProcessor.IsLastGroup && bProcessor.IsLastGroup)
					return 0;   // nothing left to compare
				else if (aProcessor.IsLastGroup && !bProcessor.IsLastGroup)
					return -1;  // a < b : b has more groups
				else if (!aProcessor.IsLastGroup && bProcessor.IsLastGroup)
					return 1;   // a > b : a has more groups


				// Both a and b have more groups to process at this point.


				/// Whitespace comparison


				var spaceComparison = aProcessor.SpaceCount.CompareTo(bProcessor.SpaceCount);
				if (spaceComparison != 0)
					return spaceComparison;


				// Both a and b have more groups to process and are are equal up to the current group at this point.


				// Move to next groups
				aProcessor.MoveNext();
				bProcessor.MoveNext();


			} while (true);
		}

		internal class GroupProcessor
		{
			readonly string _text;
			readonly int _length;
			int _index = 0;

			Regex RegexMatchDigit = new Regex(@"\d", RegexOptions.Compiled);        // Regex.Match returns all digits in string



			/// Properties



			/// <summary>The text before a number, or the entire text, or or <see cref="string.Empty"/> (if the number part is at the start)</summary>
			public string Prefix { get; private set; }

			/// <summary>The text used to work out the number part of the group</summary>
			public string TextNumber { get; private set; }

			/// <summary>The text after a number, or <see cref="string.Empty"/></summary>
			public string Suffix { get; private set; }

			/// <summary>The whole number part of the number if <see cref="IsWholeNumberPartProvided"/>.  0 otherwise.</summary>
			public int WholeNumber { get; private set; }

			/// <summary>The decimal number part of the number, as text.  Includes the decimal point.</summary>
			public string DecimalNumberText { get; private set; }

			/// <summary>The number of spaces at the end of this group, , or <see cref="string.Empty"/></summary>
			public int SpaceCount { get; private set; }

			/// <summary>true if this is the last group.</summary>
			public bool IsLastGroup { get; private set; }


			/// <summary>true if there is a number in this group</summary>
			public bool IsNumberAvailable => !TextNumber.IsNullOrWhiteSpace();

			/// <summary>true if there is a whole number part (if there is a number in the group)</summary>
			public bool IsWholeNumberPartProvided { get; private set; }

			/// <summary>true if there is a decimal part (if there is a number in the group)</summary>
			public bool IsDecimalNumberPartProvided => !DecimalNumberText.IsNullOrWhiteSpace();



			/// Lifecycle



			public GroupProcessor(string text)
			{
				_text = text.Trim().ToLower();
				if (_text == null) throw new ArgumentNullException(nameof(_text));
				_length = _text.Length;

				MoveNext();
			}



			/// Actions



			/// <summary>Processes the next group</summary>
			public void MoveNext()
			{
				char c;

				var symbolStartIndex = _index;
				var symbolLength = 0;

				var numberStartIndex = -1;
				var decimalSeparatorIndex = -1;
				var decimalPartLength = 0;

				var isReadingPrefix = true;
				var isReadingNumber = false;
				var isReadingSuffix = false;
				var isReadingWhitespace = false;

				Prefix = string.Empty;
				TextNumber = string.Empty;
				Suffix = string.Empty;
				WholeNumber = 0;
				DecimalNumberText = string.Empty;
				SpaceCount = -1;
				IsLastGroup = true;

				do
				{
					c = _text[_index];

					if (c.IsDigit())
					{
						if (isReadingPrefix)
						{
							Prefix = GetSymbolAsText();

							isReadingPrefix = false;
							isReadingNumber = true;

							numberStartIndex = _index;
							symbolStartIndex = _index;
							symbolLength = 1;
						}
						else if (isReadingNumber)
						{
							symbolLength++;
							if (decimalSeparatorIndex != -1)
								decimalPartLength++;
						}
						else if (isReadingSuffix)
							symbolLength++;
						else if (isReadingWhitespace)
						{
							IsLastGroup = false;
							break;
						}
					}
					else if (c.IsDecimalSeparator())
					{
						if (isReadingPrefix)
						{
							if (CanPeekNext(out var c2))
							{
								if (c2.IsDigit())  // treat c and c2 as number
								{
									Prefix = GetSymbolAsText();

									isReadingPrefix = false;
									isReadingNumber = true;

									decimalSeparatorIndex = _index;
									decimalPartLength = 1;

									numberStartIndex = _index;
									symbolStartIndex = _index;
									symbolLength = 1;
								}
								else     // treat both c and c2 as alpha
								{
									symbolLength += 2;
									_index++;
								}
							}
							else    // treat c as alpha
								symbolLength++;
						}
						else if (isReadingNumber)
						{
							if (decimalSeparatorIndex == -1)
							{
								if (CanPeekNext(out var c2))
								{
									if (c2.IsDigit())
									{
										decimalSeparatorIndex = _index;
										decimalPartLength = 1;
										symbolLength++;
									}
									else
									{
										decimalSeparatorIndex = _index;
										decimalPartLength = 0;
										symbolLength++;

										TextNumber = GetSymbolAsText();

										isReadingNumber = false;
										isReadingSuffix = true;

										symbolStartIndex = _index + 1;
										_index++;
										symbolLength = 1;
									}
								}
								else
									symbolLength++;
							}
							else        // treat as alpha
							{
								TextNumber = GetSymbolAsText();

								isReadingNumber = false;
								isReadingSuffix = true;

								symbolStartIndex = _index;
								symbolLength = 1;
							}
						}
						else if (isReadingSuffix)
							symbolLength++;
						else if (isReadingWhitespace)
						{
							IsLastGroup = false;
							break;
						}
					}
					else if (c.IsGroupSeparator())
					{
						if (isReadingPrefix)
							symbolLength++;
						else if (isReadingNumber)
						{
							if (CanPeekNext(out var c2))
							{
								if (!c2.IsDigit())
								{
									TextNumber = GetSymbolAsText();

									isReadingNumber = false;
									isReadingSuffix = true;

									symbolStartIndex = _index;
									symbolLength = 1;
								}
								else
								{
									symbolLength++;
									decimalPartLength++;
								}
							}
							else
							{
								TextNumber = GetSymbolAsText();

								isReadingNumber = false;
								isReadingSuffix = true;

								symbolStartIndex = _index;
								symbolLength = 1;
							}
						}
						else if (isReadingSuffix)
							symbolLength++;
						else if (isReadingWhitespace)
						{
							IsLastGroup = false;
							break;
						}
					}
					else if (c.IsWhiteSpace())
					{
						if (isReadingPrefix)
						{
							Prefix = GetSymbolAsText();

							isReadingPrefix = false;
							isReadingWhitespace = true;

							IsLastGroup = false;            // there are no trailing spaces in folder item names; there must be another group

							symbolStartIndex = _index;
							symbolLength = 1;
						}
						else if (isReadingNumber)
						{
							TextNumber = GetSymbolAsText();

							isReadingNumber = false;
							isReadingWhitespace = true;

							IsLastGroup = false;            // there are no trailing spaces in folder item names; there must be another group

							symbolStartIndex = _index;
							symbolLength = 1;
						}
						else if (isReadingSuffix)
						{
							Suffix = GetSymbolAsText();

							isReadingSuffix = false;
							isReadingWhitespace = true;

							IsLastGroup = false;            // there are no trailing spaces in folder item names; there must be another group

							symbolStartIndex = _index;
							symbolLength = 1;
						}
						else if (isReadingWhitespace)
							symbolLength++;
					}
					else    // is reading alpha
					{
						if (isReadingPrefix)
							symbolLength++;
						else if (isReadingNumber)
						{
							TextNumber = GetSymbolAsText();

							isReadingNumber = false;
							isReadingSuffix = true;

							symbolStartIndex = _index;
							symbolLength = 1;

						}
						else if (isReadingWhitespace)
						{
							IsLastGroup = false;
							break;
						}
						else    // isSuffix
							symbolLength++;
					}

					_index++;

				} while (_index < _length);


				if (isReadingPrefix)
					Prefix = GetSymbolAsText();
				else if (isReadingNumber)
					TextNumber = GetSymbolAsText();
				else if (isReadingSuffix)
					Suffix = GetSymbolAsText();
				else    // isWhitespace
					SpaceCount = symbolLength;


				if (TextNumber.Any())
				{
					if (RegexMatchDigit.Match(TextNumber).Length > 0)  // gets the digits present
					{
						var textWholeNumberLength = decimalSeparatorIndex == -1 ? TextNumber.Length : decimalSeparatorIndex - numberStartIndex;
						var textWholeNumber = _text.Substring(numberStartIndex, textWholeNumberLength).CleanseToDigits();
						IsWholeNumberPartProvided = int.TryParse(textWholeNumber, out var wholeNumber);
						WholeNumber = IsWholeNumberPartProvided ? wholeNumber : 0;

						if (decimalSeparatorIndex != -1)
							DecimalNumberText = _text.Substring(decimalSeparatorIndex, decimalPartLength).CleanseGroupSeparators();
					}
				}

				return;


				/// Local Functions


				string GetSymbolAsText() => _text.Substring(symbolStartIndex, symbolLength);

				/// <summary>True if there is a character beyond the character at the current index</summary>
				bool CanPeekNext(out char c2)
				{
					if (_index + 1 <= _length - 1)  // can peek next
					{
						c2 = _text[_index + 1];
						return true;
					}

					c2 = '/';   // invalid char in file item names
					return false;
				}
			}



			/// Overrides



			public override string ToString() =>
				$"p='{Prefix}' n='{TextNumber}' s='{Suffix}' w={(IsNumberAvailable ? WholeNumber.ToString() : string.Empty)} d='{DecimalNumberText}' sc={SpaceCount} isl={IsLastGroup}";

		}
	}
}