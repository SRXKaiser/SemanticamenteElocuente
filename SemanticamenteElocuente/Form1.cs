using System.Text;

namespace SemanticamenteElocuente
{
    public partial class Form1 : Form
    {
        private string _codigoFuente = string.Empty;
        private string _rutaArchivo = string.Empty;

        public Form1()
        {
            InitializeComponent();
            Text = "Yo y los analfabetos semanticos cuando:";

            this.BackColor = Color.FromArgb(245, 245, 250);
            this.Font = new Font("Segoe UI", 10, FontStyle.Regular);

            btnAbrir.BackColor = Color.FromArgb(52, 152, 219);
            btnAbrir.ForeColor = Color.White;
            btnAbrir.FlatStyle = FlatStyle.Flat;
            btnAbrir.FlatAppearance.BorderSize = 0;

            btnAnalizar.BackColor = Color.FromArgb(46, 204, 113);
            btnAnalizar.ForeColor = Color.White;
            btnAnalizar.FlatStyle = FlatStyle.Flat;
            btnAnalizar.FlatAppearance.BorderSize = 0;

            txtRuta.BorderStyle = BorderStyle.FixedSingle;

            rtbCodigo.BackColor = Color.White;
            rtbCodigo.BorderStyle = BorderStyle.FixedSingle;
            rtbCodigo.Font = new Font("Consolas", 11f, FontStyle.Regular);

            rtbSalida.BackColor = Color.FromArgb(250, 250, 250);
            rtbSalida.BorderStyle = BorderStyle.FixedSingle;
            rtbSalida.Font = new Font("Consolas", 10f, FontStyle.Regular);

        }

        private void btnAnalizar_Click(object sender, EventArgs e)
        {
            rtbSalida.Clear();

            _codigoFuente = rtbCodigo.Text;

            if (string.IsNullOrWhiteSpace(_codigoFuente))
            {
                rtbSalida.AppendText("No hay código cargado o escrito.\n");
                return;
            }

            try
            {
                // =========================================
                // 1. ANÁLISIS LÉXICO
                // =========================================
                var lexer = new Lexer(_codigoFuente);
                var tokens = lexer.ScanAll().ToList();

                Highlighter.Colorize(rtbCodigo, tokens);

                rtbSalida.SelectionColor = Color.Black;
                rtbSalida.AppendText("=== TOKENS ===\n");
                rtbSalida.AppendText("Línea:Col  Tipo                  Lexema               Descripción\n");
                rtbSalida.AppendText("-------------------------------------------------------------------\n");

                foreach (var t in tokens)
                {
                    rtbSalida.SelectionColor = Color.Black;
                    rtbSalida.AppendText($"{t.Line}:{t.Column,-4} {t.Type,-20} ");

                    rtbSalida.SelectionColor = ColorFor(t.Type);
                    rtbSalida.AppendText($"'{t.Lexeme}'".PadRight(20));

                    rtbSalida.SelectionColor = Color.Black;
                    rtbSalida.AppendText($"  {Descripcion(t)}\n");
                }

                // =========================================
                // 2. ANÁLISIS SINTÁCTICO
                // =========================================
                rtbSalida.AppendText("\n=== ANÁLISIS SINTÁCTICO ===\n");

                var parser = new Parser(tokens);
                var program = parser.ParseProgram();

                if (parser.Errors.Count > 0)
                {
                    rtbSalida.SelectionColor = Color.Red;
                    rtbSalida.AppendText("Se encontraron errores sintácticos:\n\n");

                    foreach (var err in parser.Errors)
                        rtbSalida.AppendText(err + "\n");

                    rtbSalida.SelectionColor = Color.Black;

                    var erroresSintacticos = parser.Errors
                        .Select(e => (e.Line, e.Column, e.Message))
                        .ToList();

                    Highlighter.Colorize(rtbCodigo, tokens, erroresSintacticos);
                    return;
                }

                rtbSalida.SelectionColor = Color.DarkGreen;
                rtbSalida.AppendText("Análisis sintáctico correcto.\n");

                // =========================================
                // 3. ANÁLISIS SEMÁNTICO
                // =========================================
                rtbSalida.AppendText("\n=== ANÁLISIS SEMÁNTICO ===\n");

                var semantic = new SemanticAnalyzer();
                var semanticErrors = semantic.Analyze(program);

                if (semanticErrors.Count > 0)
                {
                    rtbSalida.SelectionColor = Color.DarkRed;
                    rtbSalida.AppendText("Se encontraron errores semánticos:\n\n");

                    foreach (var err in semanticErrors)
                        rtbSalida.AppendText(err + "\n");

                    rtbSalida.SelectionColor = Color.Black;

                    var erroresSemanticos = semanticErrors
                        .Select(e => (e.Line, e.Column, e.Message))
                        .ToList();

                    Highlighter.Colorize(rtbCodigo, tokens, erroresSemanticos);
                    return;
                }

                rtbSalida.SelectionColor = Color.DarkGreen;
                rtbSalida.AppendText("Análisis semántico correcto.\n");

                // =========================================
                // 4. EJECUCIÓN
                // =========================================
                rtbSalida.AppendText("\n=== EJECUCIÓN ===\n");

                var evaluator = new Evaluator();
                var output = evaluator.Run(program);

                if (output.Count == 0)
                {
                    rtbSalida.SelectionColor = Color.DimGray;
                    rtbSalida.AppendText("(Sin salida)\n");
                }
                else
                {
                    rtbSalida.SelectionColor = Color.DarkBlue;
                    foreach (var line in output)
                        rtbSalida.AppendText(line + "\n");
                }

                rtbSalida.SelectionColor = Color.Black;

                // =========================================
                // 5. EXPORTAR CSV
                // =========================================
                if (!string.IsNullOrWhiteSpace(_rutaArchivo) && File.Exists(_rutaArchivo))
                {
                    var dir = Path.GetDirectoryName(_rutaArchivo)!;
                    var baseName = Path.GetFileNameWithoutExtension(_rutaArchivo);
                    var csvPath = Path.Combine(dir, $"tokens_{baseName}.csv");

                    var sb = new StringBuilder();
                    sb.AppendLine("Linea,Columna,Tipo,Lexema,Descripcion");

                    foreach (var t in tokens)
                    {
                        var lexemaCsv = t.Lexeme.Replace("\"", "\"\"");
                        sb.AppendLine($"{t.Line},{t.Column},{t.Type},\"{lexemaCsv}\",\"{Descripcion(t)}\"");
                    }

                    File.WriteAllText(csvPath, sb.ToString(), Encoding.UTF8);

                    rtbSalida.AppendText("\n");
                    rtbSalida.SelectionColor = Color.DarkSlateGray;
                    rtbSalida.AppendText($"[Exportación] CSV generado: {csvPath}\n");
                    rtbSalida.SelectionColor = Color.Black;
                }
            }
            catch (ParseError ex)
            {
                rtbSalida.SelectionColor = Color.Red;
                rtbSalida.AppendText("Error sintáctico fatal:\n");
                rtbSalida.AppendText(ex + "\n");
                rtbSalida.SelectionColor = Color.Black;
            }
            catch (EvalError ex)
            {
                rtbSalida.SelectionColor = Color.Red;
                rtbSalida.AppendText("Error de ejecución:\n");
                rtbSalida.AppendText(ex + "\n");
                rtbSalida.SelectionColor = Color.Black;
            }
            catch (Exception ex)
            {
                rtbSalida.SelectionColor = Color.Red;
                rtbSalida.AppendText($"Error durante el análisis: {ex.Message}\n");
                rtbSalida.SelectionColor = Color.Black;
            }
        }

        // ---- Helpers ----
        private static System.Drawing.Color ColorFor(TokenType tt) => tt switch
        {
            TokenType.Number => System.Drawing.Color.DarkBlue,
            TokenType.String => System.Drawing.Color.Brown,
            TokenType.Identifier => System.Drawing.Color.DarkGreen,

            TokenType.Var or TokenType.Let or TokenType.Const or
            TokenType.Print or TokenType.If or TokenType.Else or
            TokenType.While or TokenType.For or TokenType.Switch or
            TokenType.Case or TokenType.Default or TokenType.Break or
            TokenType.Continue or TokenType.Function or TokenType.Return or
            TokenType.True or TokenType.False
                => System.Drawing.Color.MediumVioletRed,

            TokenType.Plus or TokenType.Minus or TokenType.Star or TokenType.Slash or
            TokenType.Percent or TokenType.EqualEqual or TokenType.BangEqual or
            TokenType.Less or TokenType.LessEqual or TokenType.Greater or
            TokenType.GreaterEqual or TokenType.AndAnd or TokenType.OrOr or
            TokenType.Bang or TokenType.Increment or TokenType.Decrement
                => System.Drawing.Color.Firebrick,

            TokenType.Assign or TokenType.PlusAssign or TokenType.MinusAssign or
            TokenType.StarAssign or TokenType.SlashAssign
                => System.Drawing.Color.Sienna,

            TokenType.Semicolon or TokenType.Comma or TokenType.Colon or TokenType.Dot
                => System.Drawing.Color.SlateGray,

            TokenType.LParen or TokenType.RParen or TokenType.LBrace or TokenType.RBrace
                => System.Drawing.Color.SteelBlue,

            TokenType.Comment => System.Drawing.Color.DarkGray,
            _ => System.Drawing.Color.Black
        };

        private static string Descripcion(Token t) => t.Type switch
        {
            TokenType.Number => "Número",
            TokenType.String => "Cadena",
            TokenType.Identifier => "Identificador",

            TokenType.Var => "Palabra reservada: var",
            TokenType.Let => "Palabra reservada: let",
            TokenType.Const => "Palabra reservada: const",
            TokenType.Print => "Palabra reservada: print",
            TokenType.If => "Palabra reservada: if",
            TokenType.Else => "Palabra reservada: else",
            TokenType.While => "Palabra reservada: while",
            TokenType.For => "Palabra reservada: for",
            TokenType.Switch => "Palabra reservada: switch",
            TokenType.Case => "Palabra reservada: case",
            TokenType.Default => "Palabra reservada: default",
            TokenType.Break => "Palabra reservada: break",
            TokenType.Continue => "Palabra reservada: continue",
            TokenType.Function => "Palabra reservada: function",
            TokenType.Return => "Palabra reservada: return",
            TokenType.True => "Booleano verdadero",
            TokenType.False => "Booleano falso",

            TokenType.Plus => "Operador suma",
            TokenType.Minus => "Operador resta",
            TokenType.Star => "Operador multiplicación",
            TokenType.Slash => "Operador división",
            TokenType.Percent => "Operador módulo",
            TokenType.Increment => "Incremento",
            TokenType.Decrement => "Decremento",

            TokenType.Assign => "Asignación",
            TokenType.PlusAssign => "Asignación con suma",
            TokenType.MinusAssign => "Asignación con resta",
            TokenType.StarAssign => "Asignación con multiplicación",
            TokenType.SlashAssign => "Asignación con división",

            TokenType.EqualEqual => "Comparación igualdad",
            TokenType.BangEqual => "Comparación desigualdad",
            TokenType.Less => "Menor que",
            TokenType.LessEqual => "Menor o igual que",
            TokenType.Greater => "Mayor que",
            TokenType.GreaterEqual => "Mayor o igual que",

            TokenType.AndAnd => "AND lógico",
            TokenType.OrOr => "OR lógico",
            TokenType.Bang => "Negación lógica",

            TokenType.Semicolon => "Punto y coma",
            TokenType.Comma => "Coma",
            TokenType.Colon => "Dos puntos",
            TokenType.Dot => "Punto",

            TokenType.LParen => "Paréntesis izquierdo",
            TokenType.RParen => "Paréntesis derecho",
            TokenType.LBrace => "Llave izquierda",
            TokenType.RBrace => "Llave derecha",

            TokenType.Comment => "Comentario",
            TokenType.Whitespace => "Espacio en blanco",
            TokenType.Unknown => "Símbolo no reconocido",
            TokenType.EOF => "Fin de archivo",
            _ => ""
        };

        private void btnAbrir_Click(object sender, EventArgs e)
        {
            openFileDialog1.Title = "Selecciona el archivo de código fuente";
            openFileDialog1.Filter = "Texto|*.txt;*.code;*.src|Todos|*.*";

            if (openFileDialog1.ShowDialog(this) == DialogResult.OK)
            {
                txtRuta.Text = openFileDialog1.FileName;
                _rutaArchivo = openFileDialog1.FileName;
                _codigoFuente = File.ReadAllText(openFileDialog1.FileName, Encoding.UTF8);

                rtbCodigo.Text = _codigoFuente;  
                rtbSalida.Clear();
                rtbSalida.AppendText("Archivo cargado. Presiona \"Analizar\".\n");
            }
        }
    }
}
