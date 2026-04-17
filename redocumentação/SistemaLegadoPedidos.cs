using System;
using System.Collections.Generic;
using System.Linq;

namespace SistemaLegadoPedidos
{
    public class PedidoProcessor
    {
        private List<string> _logs = new List<string>();

        public string ProcessarPedido(
            int pedidoId,
            string nomeCliente,
            string emailCliente,
            string tipoCliente,
            List<ItemPedido> itens,
            string cupom,
            string formaPagamento,
            string enderecoEntrega,
            double pesoTotal,
            bool entregaExpressa,
            bool clienteBloqueado,
            bool enviarEmail,
            bool salvarLog,
            string pais,
            int parcelas)
        {
            string resultado = "";
            double subtotal = 0;
            double desconto = 0;
            double frete = 0;
            double juros = 0;
            double total = 0;
            bool temErro = false;

            
            // Valida identificador do pedido
            if (pedidoId <= 0)
            {
                resultado += "Pedido inválido\n";
                temErro = true;
            }

            // Valida nome do cliente
            if (nomeCliente == null || nomeCliente == "")
            {
                resultado += "Nome do cliente não informado\n";
                temErro = true;
            }

            // Email ausente gera aviso, mas não interrompe o processamento
            if (emailCliente == null || emailCliente == "")
            {
                resultado += "Email do cliente não informado\n";
            }

            // Impede processamento de cliente bloqueado
            if (clienteBloqueado == true)
            {
                resultado += "Cliente bloqueado\n";
                temErro = true;
            }

            // Valida lista de itens
            if (itens == null)
            {
                resultado += "Lista de itens nula\n";
                temErro = true;
            }
            else
            {
                if (itens.Count == 0)
                {
                    resultado += "Pedido sem itens\n";
                    temErro = true;
                }
                else
                {
                    // Percorre itens para validar quantidade, preço e compor subtotal
                    for (int i = 0; i < itens.Count; i++)
                    {
                        // Valida quantidade do item
                        if (itens[i].Quantidade <= 0)
                        {
                            resultado += "Item com quantidade inválida: " + itens[i].Nome + "\n";
                            temErro = true;
                        }

                        // Valida preço do item
                        if (itens[i].PrecoUnitario < 0)
                        {
                            resultado += "Item com preço inválido: " + itens[i].Nome + "\n";
                            temErro = true;
                        }

                        subtotal = subtotal + (itens[i].PrecoUnitario * itens[i].Quantidade);

                        // Regra de negócio: itens da categoria ALIMENTO acrescentam taxa fixa de 2
                        if (itens[i].Categoria == "ALIMENTO")
                        {
                            subtotal = subtotal + 2;
                        }

                        // Regra de negócio: itens da categoria IMPORTADO acrescentam taxa fixa de 5
                        if (itens[i].Categoria == "IMPORTADO")
                        {
                            subtotal = subtotal + 5;
                        }
                    }
                }
            }

            // Desconto base conforme perfil do cliente
            if (temErro == false)
            {
                // Valida tipo de cliente
                if (tipoCliente == "VIP")
                {
                    desconto = subtotal * 0.15;
                }
                
                else if (tipoCliente == "PREMIUM")
                {
                    desconto = subtotal * 0.10;
                }
                
                else if (tipoCliente == "NORMAL")
                {
                    desconto = subtotal * 0.02;
                }
                
                else if (tipoCliente == "NOVO")
                {
                    desconto = 0;
                }
                else
                {
                    desconto = 1;
                }

                // Aplica regras de cupom promocional
                if (cupom != null && cupom != "")
                {
                    if (cupom == "DESC10")
                    {
                        desconto = desconto + (subtotal * 0.10);
                    }
                    else if (cupom == "DESC20")
                    {
                        desconto = desconto + (subtotal * 0.20);
                    }
                    else if (cupom == "FRETEGRATIS")
                    {
                        frete = 0;
                    }
                    else if (cupom == "VIP50" && tipoCliente == "VIP")
                    {
                        desconto = desconto + 50;
                    }
                    else
                    {
                        resultado += "Cupom inválido ou não aplicável\n";
                    }
                }


                // Endereço é obrigatório para entrega
                if (enderecoEntrega == null || enderecoEntrega == "")
                {
                    resultado += "Endereço de entrega não informado\n";
                    temErro = true;
                }

                // Calcula frete nacional por peso
                if (pais == "BR")
                {
                    if (pesoTotal <= 1)
                    {
                        frete = 10;
                    }
                    else if (pesoTotal <= 5)
                    {
                        frete = 25;
                    }
                    else if (pesoTotal <= 10)
                    {
                        frete = 40;
                    }
                    else
                    {
                        frete = 70;
                    }

                    // Entrega expressa aumenta o frete
                    if (entregaExpressa == true)
                    {
                        frete = frete + 30;
                    }
                }
                else
                {
                    if (pesoTotal <= 1)
                    {
                        frete = 50;
                    }
                    else if (pesoTotal <= 5)
                    {
                        frete = 80;
                    }
                    else
                    {
                        frete = 120;
                    }

                    if (entregaExpressa == true)
                    {
                        frete = frete + 70;
                    }
                }

                // Juros no cartão por faixas de parcelamento
                if (formaPagamento == "CARTAO")
                {
                    if (parcelas > 1 && parcelas <= 6)
                    {
                        juros = subtotal * 0.02;
                    }
                    else if (parcelas > 6)
                    {
                        juros = subtotal * 0.05;
                    }
                }

                // Boleto e PIX recebem desconto fixo
                else if (formaPagamento == "BOLETO")
                {
                    desconto = desconto + 5;
                }
                else if (formaPagamento == "PIX")
                {
                    desconto = desconto + 10;
                }
                else if (formaPagamento == "DINHEIRO")
                {
                    desconto = desconto + 0;
                }
                else
                {
                    resultado += "Forma de pagamento inválida\n";
                    temErro = true;
                }

                // Calcula total final do pedido
                total = subtotal - desconto + frete + juros;

                // Impede total negativo
                if (total < 0)
                {
                    total = 0;
                }

                // Gera alertas operacionais
                if (subtotal > 1000)
                {
                    resultado += "Pedido de alto valor\n";
                }

                // Valida subtotal do pedido para clientes novos
                if (subtotal > 5000 && tipoCliente == "NOVO")
                {
                    resultado += "Pedido suspeito para cliente novo\n";
                }

                // Valida subtotal do pedido para clientes novos e forma de pagamento boleto
                if (formaPagamento == "BOLETO" && subtotal > 3000)
                {
                    resultado += "Pedido com boleto acima do limite recomendado\n";
                }

                if (pais != "BR" && subtotal < 100)
                {
                    resultado += "Pedido internacional abaixo do valor mínimo recomendado\n";
                }


                // Salva histórico de processamento
                if (salvarLog == true)
                {
                    _logs.Add("Pedido: " + pedidoId);
                    _logs.Add("Cliente: " + nomeCliente);
                    _logs.Add("Subtotal: " + subtotal);
                    _logs.Add("Desconto: " + desconto);
                    _logs.Add("Frete: " + frete);
                    _logs.Add("Juros: " + juros);
                    _logs.Add("Total: " + total);
                    _logs.Add("Data: " + DateTime.Now.ToString());
                }

                // Tenta enviar email ao cliente
                if (enviarEmail == true)
                {
                    if (emailCliente != null && emailCliente != "")
                    {
                        resultado += "Email enviado para " + emailCliente + "\n";
                    }
                    else
                    {
                        resultado += "Email não enviado: cliente sem email\n";
                    }
                }

                resultado += "TOTAL_FINAL=" + total + "\n";
            }

            return resultado;
        }

        public List<string> ObterLogs()
        {
            return _logs;
        }
    }

    public class ItemPedido
    {
        public string Nome;
        public string Categoria;
        public int Quantidade;
        public double PrecoUnitario;
    }
}