using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics.Eventing.Reader;
using System.Text.RegularExpressions;
using System.Threading;
using System.Net;
using Microsoft.VisualBasic;



namespace log_
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        #region Função de Eventos - arquivos escluidos

        Log _log;
        int qtd = 0;
        private Thread ThreadExecultar;

        private void btnListar_Click(object sender, EventArgs e)
        {
            progressBar1.Value = 0;
            btnListar.Enabled = false;
            ThreadExecultar = new Thread(lista_Filtro);
            ThreadExecultar.Start();               
        }
        
        // # Lisa os Eventos
        void lista_()
        {
            // EventLog log = EventLog.GetEventLogs().First(o => o.Log == "Security");
            try
            {
                listView1.Items.Clear(); // limpa a lista
                _log = new Log();   // cria uma instancia

                EventLog _EventLog = EventLog.GetEventLogs().First(o => o.Log == "Security"); // carrega o EventLog  Security LogApp

                foreach (EventLogEntry log in _EventLog.Entries)
                {
                    if (log.EventID.ToString() == "4656") // filtra pelo ID
                    {
                        if (File_Acao(log.Message) == true) // verifica se o Log é de Exclusão
                        {
                            // carrega a lista com os dados                            
                            _log.data = log.TimeWritten.ToString();
                            _log.Mensagem = log.Message;
                            _log.Computador = log.MachineName;
                            
                            BuscaDadosDiretorio(log.Message); // busca o diretorio do arquivo excluido
                            BuscaDadosNome(log.Message); // busca o nome do usuario
                            qtd++;
                            AddList(_log); // adiciona no listView1                        
                        }
                    }
                }
                if (qtd == 0)
                {
                    MessageBox.Show("Nenhum evento de exclusão foi encontrado !");
                }
                btnListar.Enabled = true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        bool DataLogs(string DadaInicio, string DataFim, string DataLog)
        {
            DateTime Inicio = Convert.ToDateTime(DadaInicio);
            DateTime Fim = Convert.ToDateTime(DataFim);
            DateTime DataAtual = Convert.ToDateTime(DataLog);
            bool retorno = false;

            if (DataAtual >= Inicio && DataAtual <= Fim)
            {
                retorno = true;
            }

            return retorno;
        }

        void lista_Filtro()
        {
            // EventLog log = EventLog.GetEventLogs().First(o => o.Log == "Security");
            try
            {
                listView1.Items.Clear(); // limpa a lista
                _log = new Log();   // cria uma instancia
                
                EventLog _EventLog3 = (EventLog)EventLog.GetEventLogs().First(o => o.Log == "Security");//.Entries.Cast<EventLogEntry>().Reverse();

                int total;
                total = _EventLog3.Entries.Count;
                progressBar1.Maximum = total;

                List<EventLogEntry> lista = _EventLog3.Entries.Cast<EventLogEntry>().ToList();

                // deseja ordenar por  eventID ou index?
                var listaOrdernadaInverso = lista.OrderByDescending(d => d.EventID); // orderna pelo
                //var listaOrdernadaInverso = lista.OrderByDescending(d => d.Index); // ordena pelo index

                foreach (EventLogEntry log in listaOrdernadaInverso)
                {
                    progressBar1.Value++;
                    if (log.EventID.ToString() == "4656") // filtra pelo ID
                    {
                        // filtro por Data
                        if (radioButtonData.Checked == true)
                        {
                            if (DataLogs(textBoxDataInicio.Text, textBoxDataFim.Text, log.TimeWritten.ToShortDateString()) == true)
                            {
                                if (File_Acao(log.Message) == true) // verifica se o Log é de Exclusão
                                {
                                    // carrega a lista com os dados                            
                                    _log.data = log.TimeWritten.ToString();
                                    _log.Mensagem = log.Message;
                                    _log.Computador = log.MachineName;

                                    BuscaDadosDiretorio(log.Message); // busca o diretorio do arquivo excluido
                                    BuscaDadosNome(log.Message); // busca o nome do usuario
                                    qtd++;
                                    AddList(_log); // adiciona no listView1
                                    labelQTD.Text = "QTD: " + qtd.ToString();
                                }
                            }
                        }

                        if (radioButtonNull.Checked == true)
                        {
                            if (File_Acao(log.Message) == true) // verifica se o Log é de Exclusão
                            {
                                // carrega a lista com os dados                            
                                _log.data = log.TimeWritten.ToString();
                                _log.Mensagem = log.Message;
                                _log.Computador = log.MachineName;

                                BuscaDadosDiretorio(log.Message); // busca o diretorio do arquivo excluido
                                BuscaDadosNome(log.Message); // busca o nome do usuario
                                qtd++;
                                AddList(_log); // adiciona no listView1
                                labelQTD.Text = "QTD: " + qtd.ToString();
                            }
                        }
                    }
                }     
                
                if (qtd == 0)
                {
                    MessageBox.Show("Nenhum evento de exclusão foi encontrado !");
                }
                btnListar.Enabled = true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }




        // # adiciona no listView1
        void AddList(Log dados)
        {
            ListViewItem item = new ListViewItem(new[] {dados.data, dados.Usuario,dados.Computador,dados.Arquivo ,dados.Mensagem });
            listView1.Items.Add(item);
        }

        // # Class
        public class Log 
        {
           public string data;
           public string Usuario;
           public string Arquivo;
           public string Computador;
           public string Mensagem; 
        }

        // # faz a verificação se o log é de um ARQUIVO deletado
        public bool File_Acao(string dado)
        {
           // return tipo_(dado);

           // return false;          

            // if (!dado.Contains("%%1541") == true) 
            if (dado.Contains("%%1538") == true) // se existir texto então não foi excluido, so foi solicitado
            {
                return  tipo_(dado);
            }
            else
            {
                // Objeto não foi excluido
                return false;
            }
            return false;
        }

        // # Busca o diretorio do arquivo
        void BuscaDadosDiretorio(string dadosBrutos)
        {
            int Possicao = dadosBrutos.IndexOf("Nome do Objeto:") + 17; // busca a possição da palavra

            string Dados = dadosBrutos.Substring(Possicao); // carrega o texto aparti da possição           
            string[] spli_ = Dados.Split('\t'); // divide o texto
            string diretorio = spli_[0].Remove(spli_[0].Length - 2);

            _log.Arquivo = diretorio;
        }

        // # Busca o nome do usuario
        void BuscaDadosNome(string dadosBrutos)
        {
            int Possicao = dadosBrutos.IndexOf("Nome da Conta:") + 16; // busca a possição da palavra

            string Dados = dadosBrutos.Substring(Possicao); // carrega o texto aparti da possição           
            string[] spli_ = Dados.Split('\t'); // divide o texto
            string diretorio = spli_[0].Remove(spli_[0].Length - 2);

            _log.Usuario = diretorio;
        }

        void BuscaDadosTipo(string dadosBrutos)
        {
            int Possicao = dadosBrutos.IndexOf("Tipo de Objeto:") + 17; // busca a possição da palavra

            string Dados = dadosBrutos.Substring(Possicao); // carrega o texto aparti da possição           
            string[] spli_ = Dados.Split('\t'); // divide o texto
            string Tipo = spli_[0].Remove(spli_[0].Length - 2);                      
        }

        public bool tipo_(string dadosBrutos)
        {
            int Possicao = dadosBrutos.IndexOf("Tipo de Objeto:") + 17; // busca a possição da palavra

            string Dados = dadosBrutos.Substring(Possicao); // carrega o texto aparti da possição           
            string[] spli_ = Dados.Split('\t'); // divide o texto
            string Tipo = spli_[0].Remove(spli_[0].Length - 2);
            if (Tipo == "File")
            {
                return true;
            }
            else
            {
                return false;
            }
            return false;
        }
           
        #endregion
        
        #region Eventos do listView

        private void listView1_Click(object sender, EventArgs e)
        {
            textBox1.Text = listView1.FocusedItem.SubItems[4].Text;
        }
        private void listView1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                textBox1.Text = listView1.FocusedItem.SubItems[4].Text;
            }
        }              
        private void listView1_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            try
            {
                textBox1.Text = listView1.FocusedItem.SubItems[4].Text;
            }
            catch
            { }
        }

        #endregion

    }
}
