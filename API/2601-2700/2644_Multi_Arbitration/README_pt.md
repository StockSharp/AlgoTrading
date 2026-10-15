# Estratégia Multi Arbitragem
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia Multi Arbitragem** é um port StockSharp do consultor especialista MetaTrader "Multi_arbitration 1.000". O script original avalia continuamente as posições compradas e vendidas existentes, adiciona novos trades na direção com menor lucro flutuante, e realiza uma liquidação global assim que os objetivos gerais de lucro são atingidos. Esta implementação em C# mantém a lógica de decisão central, adaptando-a ao modelo de portfolio de compensação do StockSharp e à API de estratégias de alto nível.

A estratégia:
- Abre uma posição comprada inicial assim que a primeira vela finalizada chega.
- Compara o lucro não realizado da direção ativa com a direção alternativa para decidir se uma reversão é necessária.
- Força uma posição plana quando o objetivo de lucro configurado é excedido ou quando a pressão de posição cresce além de um limite configurável.
- Usa apenas ordens de mercado (`BuyMarket` / `SellMarket`) para manter simplicidade e execução rápida.

## Lógica de Trading
1. **Ordem inicial** – A primeira vela finalizada aciona uma ordem de mercado comprada com o volume de operação configurado. Isso reproduz a entrada imediata no mercado do consultor especialista MetaTrader.
2. **Comparação de lucros** – Em cada vela finalizada, a estratégia calcula o PnL flutuante da direção atual:
   - Lucro comprado = `(close - entry) * volume`
   - Lucro vendido = `(entry - close) * volume`
3. **Seleção de posição** – Se a direção alternativa funcionaria melhor atualmente que a ativa, a estratégia inverte a posição enviando uma ordem de mercado dimensionada para cobrir a exposição existente e abrir uma nova posição na nova direção. Quando nenhuma posição está aberta, o algoritmo padroniza para uma entrada comprada, correspondendo ao consultor especialista original.
4. **Guarda de limite de posições** – Um parâmetro configurável `MaxOpenPositions` espelha a verificação MetaTrader contra `LimitOrders()`. Quando a exposição combinada comprada/vendida atinge esse limite e a estratégia é lucrativa, ela aplaina o livro para evitar alavancagem excessiva.
5. **Saída por objetivo de lucro** – Quando o PnL da conta (realizado + não realizado) excede o limite `ProfitForClose`, a estratégia fecha todas as posições, exatamente como a verificação original `Equity - Balance`.

## Parâmetros
| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `TradeVolume` | Volume usado para cada ordem de mercado. Representa o tamanho mínimo de lote no EA original. | `1` |
| `ProfitForClose` | Limite de lucro que aciona uma saída global quando excedido. | `300` |
| `MaxOpenPositions` | Número máximo de posições simultâneas permitidas antes de a estratégia forçar um aplainamento. Equivale a `limit - 15`. | `15` |
| `CandleType` | Tipo de dados de vela que sincroniza as decisões de operação. Padrão é período de 1 minuto. | `velas de 1 minuto` |

## Notas de Implementação
- StockSharp usa um modelo de posição de compensação, portanto a estratégia só pode manter uma direção líquida por vez. As reversões são tratadas dimensionando ordens de mercado para fechar a exposição existente e abrir uma nova posição na direção oposta.
- A chamada `StartProtection()` é usada para herdar o gerenciamento de risco integrado (por exemplo, stop-out em posições diferentes de zero quando a estratégia é parada).
- Todas as variáveis de estado (`_entryPrice`, `_currentSide`, `_initialOrderPlaced`) são redefinidas em `OnReseted` para suportar reinicializações e simulações repetidas sem dados obsoletos.
- A estratégia só reage a **velas finalizadas** para evitar a contagem dupla de lucros em barras parcialmente formadas.

## Recomendações de Uso
- Alinhe o parâmetro `TradeVolume` com o tamanho do lote do instrumento ou o multiplicador do contrato.
- O valor `ProfitForClose` deve ser definido usando a mesma moeda que o PnL da conta (por exemplo, USD para contas FX).
- Aumente ou diminua `MaxOpenPositions` dependendo de quão agressivamente você quer que a estratégia acumule exposição antes de forçar um aplainamento.
- Como a estratégia sempre começa com uma operação comprada, considere iniciá-la manualmente quando entradas compradas são aceitáveis para o instrumento negociado.

## Diferenças da Versão MetaTrader
- O modo de cobertura do MetaTrader permite posições compradas e vendidas simultâneas, enquanto este port opera em um ambiente de compensação. A lógica de decisão ainda compara a rentabilidade direcional, mas apenas uma posição líquida é mantida a qualquer momento.
- Verificações específicas da plataforma (permissões de trading do terminal, seleção do tipo de preenchimento, números mágicos da conta) são substituídas por equivalentes StockSharp como `StartProtection()` e assinaturas de velas.
- Diagnósticos comentados do arquivo MQL não são reproduzidos; confie no log do StockSharp se informações em tempo de execução forem necessárias.
