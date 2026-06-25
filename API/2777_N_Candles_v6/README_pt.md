# Estratégia N Candles v6
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia **N Candles v6** monitora as velas concluídas mais recentes e procura sequências de direção idêntica. Quando o mercado imprime `N` velas de alta seguidas, a estratégia abre uma posição comprada, enquanto uma série de `N` velas de baixa produz uma entrada vendida. A lógica é inspirada no consultor especialista do MetaTrader *N Candles v6.mq5* e é adaptada para a API de alto nível do StockSharp.

O algoritmo é projetado para qualquer símbolo que entregue velas padrão baseadas em tempo. Uma janela de trading configurável mantém a estratégia inativa fora da sessão desejada, mas a lógica ativa de trailing e saída continua a proteger uma posição aberta mesmo durante as horas bloqueadas.

## Lógica de trading
1. Subscrever ao tipo de vela configurado e processar apenas velas concluídas.
2. Contar velas consecutivas de alta (`Close > Open`) e de baixa (`Close < Open`). Dojis reiniciam os contadores.
3. Quando `CandlesCount` velas de alta aparecerem:
   - Verificar que a posição líquida projetada permaneça abaixo de `MaxPositionVolume`.
   - Enviar uma ordem de compra de mercado. Se uma posição vendida existir, o tamanho da ordem é aumentado para virar a posição para comprada em um único trade.
4. Quando `CandlesCount` velas de baixa aparecerem:
   - Assegurar que a nova exposição vendida não excederá `MaxPositionVolume`.
   - Enviar uma ordem de venda de mercado e ampliar a ordem se uma posição comprada precisar ser fechada.
5. Se a vela mais recente quebrar a sequência (a "ovelha negra"):
   - Aplicar o `ClosingMode` selecionado para fechar todas, as opostas ou as da mesma direção de uma vez.
6. O trailing e as saídas protetoras são executados em cada vela:
   - Os níveis de stop-loss e take-profit são derivados de distâncias em pips e do passo de preço do instrumento.
   - O trailing stop se ativa após o preço se mover por `TrailingStopPips + TrailingStepPips` e apenas se engancha na direção favorável.
   - Qualquer violação do stop, take-profit, ou nível de trailing fecha imediatamente toda a posição.

## Gerenciamento de risco
- **Stop Loss (pips)** – converte a distância em pips em um offset de preço absoluto usando o passo de preço do símbolo (instrumentos de 5 e 3 dígitos são escalonados automaticamente).
- **Take Profit (pips)** – fecha a posição após um movimento favorável do tamanho especificado.
- **Trailing Stop / Step (pips)** – habilita proteção dinâmica assim que o trade atinge o limiar de lucro configurado. O passo deve ser diferente de zero quando o trailing está ativo.
- **Max Position Volume** – limita a posição líquida absoluta. Sinais que violariam o limite são ignorados.
- **Closing Mode** – determina como reagir quando uma vela não conforme aparece:
  - `All` – zerar toda a posição.
  - `Opposite` – fechar posições contra a direção da sequência (p.ex., fechar vendidas após uma sequência de alta se quebrar).
  - `Unidirectional` – fechar posições apenas na direção da sequência.
- **Janela de trading** – a estratégia abre novos trades apenas quando a hora de abertura da vela está entre `StartHour` e `EndHour` (inclusive). As saídas protetoras continuam operando mesmo quando novos trades estão bloqueados.

## Parâmetros
| Nome | Padrão | Descrição |
|------|--------|-----------|
| `CandlesCount` | 3 | Número de velas idênticas necessárias para um sinal. |
| `OrderVolume` | 0.01 | Tamanho base da ordem de mercado. A exposição oposta é fechada antes de estabelecer um novo trade. |
| `TakeProfitPips` | 50 | Distância do take-profit em pips. `0` desabilita o alvo. |
| `StopLossPips` | 50 | Distância do stop-loss em pips. `0` desabilita o stop. |
| `TrailingStopPips` | 10 | Distância do trailing stop em pips. `0` desabilita o trailing. |
| `TrailingStepPips` | 4 | Melhoria mínima de preço antes que o nível de trailing se mova. Deve ser > 0 quando o trailing está habilitado. |
| `MaxPositionVolume` | 2 | Máxima posição líquida absoluta. |
| `UseTradingHours` | true | Habilita filtragem de janela de trading. |
| `StartHour` | 11 | Início da sessão de trading (0-23). |
| `EndHour` | 18 | Fim da sessão de trading (0-23). |
| `ClosingMode` | All | Comportamento quando uma vela ovelha negra aparece. |
| `CandleType` | Velas de 1 hora | Tipo de dados usado para geração de sinais. |

## Notas
- A conversão de pips é baseada no `PriceStep` do instrumento. Para cotações de 5 e 3 dígitos, a estratégia multiplica o passo por dez para corresponder à definição tradicional de pip.
- Chamar `StartProtection()` durante o início para habilitar os serviços de salvaguarda do StockSharp (cancelar-ao-parar, segurança de reconexão, etc.).
- A lógica usa a posição líquida (`Strategy.Position`) e portanto opera corretamente em contas de netting. O comportamento estilo hedging pode ser emulado definindo um `MaxPositionVolume` alto.
