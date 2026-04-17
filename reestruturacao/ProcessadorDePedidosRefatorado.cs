using System;
using System.Collections.Generic;
using System.Text;

namespace SistemaLegadoPedidos
{
    public class ProcessadorDePedidosRefatorado
    {
        private readonly List<string> _logs = new List<string>();
        private readonly ValidadorDeEntrada _inputValidator = new ValidadorDeEntrada();
        private readonly ValidadorDeItensECalculadoraDeSubtotal _itemsValidatorAndSubtotalCalculator = new ValidadorDeItensECalculadoraDeSubtotal();
        private readonly CalculadoraDeDesconto _discountCalculator = new CalculadoraDeDesconto();
        private readonly AplicadorDeCupom _couponApplier = new AplicadorDeCupom();
        private readonly CalculadoraDeFrete _shippingCalculator = new CalculadoraDeFrete();
        private readonly AjustadorDePagamento _paymentAdjuster = new AjustadorDePagamento();
        private readonly GeradorDeAlertas _alertGenerator = new GeradorDeAlertas();
        private readonly RegistradorDeLogs _logRecorder = new RegistradorDeLogs();
        private readonly ServicoDeEmail _emailService = new ServicoDeEmail();

        public string ProcessarPedido(
            int orderId,
            string customerName,
            string customerEmail,
            string customerType,
            List<ItemPedido> items,
            string coupon,
            string paymentMethod,
            string deliveryAddress,
            double totalWeight,
            bool expressDelivery,
            bool isCustomerBlocked,
            bool shouldSendEmail,
            bool shouldSaveLog,
            string country,
            int installments)
        {
            // Pipeline de processamento:
            // 1) validações de entrada e itens
            // 2) cálculos (desconto, cupom, frete, juros) e total
            // 3) alertas, log e email
            var result = new ResultadoDeProcessamento();

            bool hasError = _inputValidator.Validar(orderId, customerName, customerEmail, isCustomerBlocked, result);

            double subtotal = 0;
            hasError = _itemsValidatorAndSubtotalCalculator.ValidarECalcular(items, result, ref subtotal) || hasError;

            if (hasError)
            {
                return result.ToString();
            }

            // Desconto base por tipo de cliente, depois ajustes por cupom e forma de pagamento.
            double discount = _discountCalculator.CalcularDescontoBase(subtotal, customerType);
            double shipping = 0;
            double interest = 0;

            _couponApplier.Aplicar(coupon, customerType, subtotal, result, ref discount, ref shipping);

            // Endereço é obrigatório. A regra original apenas adiciona mensagem; não interrompe o fluxo.
            if (string.IsNullOrEmpty(deliveryAddress))
            {
                result.AdicionarLinha("Endereço de entrega não informado");
            }

            // Frete calculado por país, peso e opção de entrega.
            shipping = _shippingCalculator.Calcular(country, totalWeight, expressDelivery);

            // Juros e descontos adicionais dependem da forma de pagamento/parcelas.
            _paymentAdjuster.Ajustar(paymentMethod, installments, subtotal, result, ref discount, ref interest);

            double total = subtotal - discount + shipping + interest;
            if (total < 0)
            {
                total = 0;
            }

            // Alertas operacionais (não bloqueantes).
            _alertGenerator.Gerar(subtotal, customerType, paymentMethod, country, result);

            if (shouldSaveLog)
            {
                _logRecorder.Registrar(_logs, orderId, customerName, subtotal, discount, shipping, interest, total);
            }

            if (shouldSendEmail)
            {
                _emailService.TentarEnviar(customerEmail, result);
            }

            // Total final sempre é emitido ao final do processamento (como no legado).
            result.AdicionarLinha("TOTAL_FINAL=" + total);

            return result.ToString();
        }

        public List<string> ObterLogs()
        {
            return _logs;
        }
    }

    internal sealed class ResultadoDeProcessamento
    {
        private readonly StringBuilder _output = new StringBuilder();

        public void AdicionarLinha(string text)
        {
            _output.Append(text).Append('\n');
        }

        public override string ToString()
        {
            return _output.ToString();
        }
    }

    internal sealed class ValidadorDeEntrada
    {
        public bool Validar(int orderId, string customerName, string customerEmail, bool isCustomerBlocked, ResultadoDeProcessamento result)
        {
            bool hasError = false;

            // Validações de entrada (regras bloqueantes).
            if (orderId <= 0)
            {
                result.AdicionarLinha("Pedido inválido");
                hasError = true;
            }

            if (string.IsNullOrEmpty(customerName))
            {
                result.AdicionarLinha("Nome do cliente não informado");
                hasError = true;
            }

            // Email ausente gera aviso, mas não bloqueia.
            if (string.IsNullOrEmpty(customerEmail))
            {
                result.AdicionarLinha("Email do cliente não informado");
            }

            if (isCustomerBlocked)
            {
                result.AdicionarLinha("Cliente bloqueado");
                hasError = true;
            }

            return hasError;
        }
    }

    internal sealed class ValidadorDeItensECalculadoraDeSubtotal
    {
        public bool ValidarECalcular(List<ItemPedido> items, ResultadoDeProcessamento result, ref double subtotal)
        {
            bool hasError = false;

            // Validação de itens e composição do subtotal.
            if (items == null)
            {
                result.AdicionarLinha("Lista de itens nula");
                return true;
            }

            if (items.Count == 0)
            {
                result.AdicionarLinha("Pedido sem itens");
                return true;
            }

            for (int i = 0; i < items.Count; i++)
            {
                ItemPedido item = items[i];

                if (item.Quantity <= 0)
                {
                    result.AdicionarLinha("Item com quantidade inválida: " + item.Name);
                    hasError = true;
                }

                if (item.UnitPrice < 0)
                {
                    result.AdicionarLinha("Item com preço inválido: " + item.Name);
                    hasError = true;
                }

                subtotal += item.UnitPrice * item.Quantity;

                // Regras adicionais por categoria.
                if (item.Category == "ALIMENTO")
                {
                    subtotal += 2;
                }

                if (item.Category == "IMPORTADO")
                {
                    subtotal += 5;
                }
            }

            return hasError;
        }
    }

    internal sealed class CalculadoraDeDesconto
    {
        public double CalcularDescontoBase(double subtotal, string customerType)
        {
            // Desconto base por perfil de cliente.
            if (customerType == "VIP")
            {
                return subtotal * 0.15;
            }

            if (customerType == "PREMIUM")
            {
                return subtotal * 0.10;
            }

            if (customerType == "NORMAL")
            {
                return subtotal * 0.02;
            }

            if (customerType == "NOVO")
            {
                return 0;
            }

            return 1;
        }
    }

    internal sealed class AplicadorDeCupom
    {
        public void Aplicar(string coupon, string customerType, double subtotal, ResultadoDeProcessamento result, ref double discount, ref double shipping)
        {
            // Cupom ajusta desconto ou frete.
            if (string.IsNullOrEmpty(coupon))
            {
                return;
            }

            if (coupon == "DESC10")
            {
                discount += subtotal * 0.10;
                return;
            }

            if (coupon == "DESC20")
            {
                discount += subtotal * 0.20;
                return;
            }

            if (coupon == "FRETEGRATIS")
            {
                shipping = 0;
                return;
            }

            if (coupon == "VIP50" && customerType == "VIP")
            {
                discount += 50;
                return;
            }

            result.AdicionarLinha("Cupom inválido ou não aplicável");
        }
    }

    internal sealed class CalculadoraDeFrete
    {
        public double Calcular(string country, double totalWeight, bool expressDelivery)
        {
            // Frete por país e faixas de peso, com adicional para entrega expressa.
            double shipping;

            if (country == "BR")
            {
                if (totalWeight <= 1)
                {
                    shipping = 10;
                }
                else if (totalWeight <= 5)
                {
                    shipping = 25;
                }
                else if (totalWeight <= 10)
                {
                    shipping = 40;
                }
                else
                {
                    shipping = 70;
                }

                if (expressDelivery)
                {
                    shipping += 30;
                }

                return shipping;
            }

            if (totalWeight <= 1)
            {
                shipping = 50;
            }
            else if (totalWeight <= 5)
            {
                shipping = 80;
            }
            else
            {
                shipping = 120;
            }

            if (expressDelivery)
            {
                shipping += 70;
            }

            return shipping;
        }
    }

    internal sealed class AjustadorDePagamento
    {
        public void Ajustar(
            string paymentMethod,
            int installments,
            double subtotal,
            ResultadoDeProcessamento result,
            ref double discount,
            ref double interest)
        {
            // Ajustes por forma de pagamento: juros no cartão e descontos fixos em boleto/pix.
            if (paymentMethod == "CARTAO")
            {
                if (installments > 1 && installments <= 6)
                {
                    interest = subtotal * 0.02;
                }
                else if (installments > 6)
                {
                    interest = subtotal * 0.05;
                }

                return;
            }

            if (paymentMethod == "BOLETO")
            {
                discount += 5;
                return;
            }

            if (paymentMethod == "PIX")
            {
                discount += 10;
                return;
            }

            if (paymentMethod == "DINHEIRO")
            {
                discount += 0;
                return;
            }

            result.AdicionarLinha("Forma de pagamento inválida");
        }
    }

    internal sealed class GeradorDeAlertas
    {
        public void Gerar(double subtotal, string customerType, string paymentMethod, string country, ResultadoDeProcessamento result)
        {
            // Alertas não interrompem o fluxo e apenas adicionam mensagens ao resultado.
            if (subtotal > 1000)
            {
                result.AdicionarLinha("Pedido de alto valor");
            }

            if (subtotal > 5000 && customerType == "NOVO")
            {
                result.AdicionarLinha("Pedido suspeito para cliente novo");
            }

            if (paymentMethod == "BOLETO" && subtotal > 3000)
            {
                result.AdicionarLinha("Pedido com boleto acima do limite recomendado");
            }

            if (country != "BR" && subtotal < 100)
            {
                result.AdicionarLinha("Pedido internacional abaixo do valor mínimo recomendado");
            }
        }
    }

    internal sealed class RegistradorDeLogs
    {
        public void Registrar(
            List<string> logs,
            int orderId,
            string customerName,
            double subtotal,
            double discount,
            double shipping,
            double interest,
            double total)
        {
            // Mantém o mesmo conteúdo textual dos logs do legado.
            logs.Add("Pedido: " + orderId);
            logs.Add("Cliente: " + customerName);
            logs.Add("Subtotal: " + subtotal);
            logs.Add("Desconto: " + discount);
            logs.Add("Frete: " + shipping);
            logs.Add("Juros: " + interest);
            logs.Add("Total: " + total);
            logs.Add("Data: " + DateTime.Now.ToString());
        }
    }

    internal sealed class ServicoDeEmail
    {
        public void TentarEnviar(string customerEmail, ResultadoDeProcessamento result)
        {
            // Envio simulado (preserva mensagens do legado).
            if (!string.IsNullOrEmpty(customerEmail))
            {
                result.AdicionarLinha("Email enviado para " + customerEmail);
                return;
            }

            result.AdicionarLinha("Email não enviado: cliente sem email");
        }
    }

    public class ItemPedido
    {
        public string Name;
        public string Category;
        public int Quantity;
        public double UnitPrice;
    }
}
