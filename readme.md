# Atividade de Engenharia Reversa e Refatoração

## Objetivo
Realizar a engenharia reversa do código legado disponibilizado no repositório da atividade, identificando responsabilidades, regras de negócio implícitas, significado dos parâmetros, oportunidades de redocumentação e propostas de reestruturação.

## Parte 01 - Reverse Engineering
A classe analisada foi `PedidoProcessor`, responsável pelo processamento de pedidos. Durante a análise foi identificado que a classe não apenas processa pedidos, mas também realiza:
- validações de entrada;
- cálculo de subtotal;
- aplicação de descontos;
- cálculo de frete nacional e internacional;
- cálculo de juros por parcelamento;
- geração de alertas;
- envio de email;
- persistência de logs.

## Parte 02 - Redocumentação
Foi realizada:
- melhoria dos nomes de variáveis, métodos e classe;
- inserção de comentários explicativos no código original;
- elaboração de diagrama de classes representando o domínio existente.

## Parte 03 - Reestruturação
Foi proposta a separação das responsabilidades em componentes específicos, reduzindo o acoplamento e aumentando a clareza lógica do sistema. A principal melhoria sugerida foi a criação de serviços especializados para validação, cálculo, notificação e logging.

## Conclusão
A análise evidenciou que o sistema legado possui baixa coesão e excesso de responsabilidades centralizadas. A refatoração proposta melhora legibilidade, manutenção, testabilidade e evolução futura do software.