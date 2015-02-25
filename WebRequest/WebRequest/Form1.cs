using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WebRequest
{
    public partial class Form1 : Form
    {
        String SeitenQuellCode { get; set; }
        String Zeichenkette { get; set; }
        Int32 Seite { get; set; }
        Boolean SeitenAnzalende { get; set; }

        List<String> ListOfPlayers { get; set; }
        List<String> ListOfCurrentPlayerInDataBase { get; set; }
        public Form1()
        {
            InitializeComponent();
            Seite = 1;
            textBox1.Text = "http://www.futhead.com/15/players/?page=" + Seite + "&sort_direction=desc";
            Zeichenkette = "<a href=\"/15/players/";
            SeitenAnzalende = false;
            ListOfPlayers = new List<string>();
            ListOfCurrentPlayerInDataBase = new List<string>();
        }

        private void btnSearch_Click(object sender, EventArgs e)
        {
            List<String> list = new List<string>();
            while (!SeitenAnzalende)
            {
                SeitenQuellCode = getHTML(textBox1.Text);
                if (null == SeitenQuellCode)
                {
                    SeitenAnzalende = true;
                    break;
                }
                textBox2.Text = SeitenQuellCode;

                list.AddRange(SearchForString());
                if (list.Count > 0)
                {
                    textBox2.Text = String.Empty;
                    textBox2.Text += String.Format("{0} Spieler gefunden" + Environment.NewLine, list.Count);
                    foreach (String item in list)
                    {
                        // - wird gegen ein " " getauscht 
                        String temp = item.Replace("-", " ");

                        // Anfangsbuchstabe von Vorname und Nachname werden groß gemacht
                        //listBoxPlayers.Items.Add(temp);

                        ListOfPlayers.Add(temp);

                        //Debugging
                        //if (ListOfPlayers.Count == 50)
                        //{
                        //    SeitenAnzalende = true;
                        //}
                    }
                    list.Clear();

                    Seite++;
                    Int32 percent = 100 * Seite / 246;
                    this.Text = Seite.ToString() + " " + percent + "%";

                    textBox1.Text = "http://www.futhead.com/15/players/?page=" + Seite + "&sort_direction=desc";
                }
                else
                {
                    SeitenAnzalende = true;
                }
            }
        }


        public string getHTML(string url)
        {
            //Anfrage an die Übergebene URL starten
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
            String html = "";
            try
            {
                //Antwort-Objekt erstellen
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                //Antwort Stream an Streamreader übergeben
                StreamReader sr = new StreamReader(response.GetResponseStream());

                //Antwort (HTML Code) auslesen
                html = sr.ReadToEnd();

                //Streamreader und Webanfrage schließen
                sr.Close();
                response.Close();
            }
            catch (Exception)
            {
                return null;
            }


            //Quellcode zurückgeben
            return html;
        }

        private List<String> SearchForString()
        {
            List<String> list = new List<String>();
            int findname_number = 0;
            while ((findname_number = SeitenQuellCode.IndexOf(Zeichenkette)) != 0)
            {

                if (findname_number < 0)
                {
                    return list;
                }
                else
                {
                    string substring = SeitenQuellCode.Substring(findname_number, 21); // Zeichenkette wird gespeichert
                    SeitenQuellCode = SeitenQuellCode.Remove(0, findname_number + 21); // Alles, vor und einschließlich der Zeichenkette wird gelöscht
                    //list.Add(substring);

                    Int32 count = 0;
                    String tempString = String.Empty;

                    foreach (Char item in SeitenQuellCode)
                    {
                        if (char.IsNumber(item))
                        {
                            count++;

                        }
                        else
                        {
                            if (count > 0)
                            {
                                String name = "";
                                foreach (Char element in SeitenQuellCode)
                                {
                                    name += element;
                                    if (name.Contains("/"))
                                    {
                                        SeitenQuellCode = SeitenQuellCode.Remove(0, name.Length);
                                        if (!char.IsNumber(name[0]))
                                        {

                                            String temp = name.Remove(name.Length - 1);
                                            temp = temp.Replace("-", " ");

                                            if (null == ListOfCurrentPlayerInDataBase.SingleOrDefault(c=> c == temp))
                                            {
                                                //if (ValidatePlayer(temp))
                                                //{
                                                //    ;
                                                //}
                                                //else // Wurde "false" zurückgegeben, so gibt es noch keinen Eintrag mit diesem Namen in der DB --> Füge der Liste hinzu
                                                //{
                                                //    list.Add(temp);
                                                //    ListOfCurrentPlayerInDataBase.Add(temp);
                                                //}

                                                list.Add(temp);
                                                ListOfCurrentPlayerInDataBase.Add(temp);
                                            }

                                            name = "";
                                            break;
                                        }
                                        name = "";
                                        continue;
                                    }
                                }
                                //SeitenQuellCode = SeitenQuellCode.Remove(0, findname_number +21);

                            }
                            break;
                        }
                    }
                }
            }
            return list;
        }

        private Boolean ValidatePlayer(String playerName)
        {
            WebRequest.Linq.PlayerDataContext context = new Linq.PlayerDataContext(WebRequest.Properties.Settings.Default.FutheadConnectionString3);
            var temp = context.Players.SingleOrDefault(c => c.Name == playerName);

            // Wenn ein Spieler mit dem Namen in der DB gefunden wurde - gib False zurück
            if (temp == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private void BtnPlayerToDB_Click(object sender, EventArgs e)
        {
            WebRequest.Linq.PlayerDataContext context = new Linq.PlayerDataContext(WebRequest.Properties.Settings.Default.FutheadConnectionString3);

            List<WebRequest.Linq.Player> linqPlayers = new List<Linq.Player>();

            try
            {
                foreach (String item in ListOfPlayers)
                {
                    WebRequest.Linq.Player player = new Linq.Player();
                    player.Name = item;
                    linqPlayers.Add(player);
                    if (linqPlayers.Count > 100)
                    {
                        context.Players.InsertAllOnSubmit(linqPlayers);
                        context.SubmitChanges();
                        linqPlayers.Clear();
                    }
                }

                context.Players.InsertAllOnSubmit(linqPlayers);
                context.SubmitChanges();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Fehler: " + ex.Message);
            }
        }
    }
}
