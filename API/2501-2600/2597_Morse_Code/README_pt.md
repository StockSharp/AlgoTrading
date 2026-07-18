# Estratégia Morse Code
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A Estratégia Morse Code replica o expert original do MetaTrader 5 que trata cada candle concluído como um "traço" ou um "ponto". Um candle de alta (preço de fechamento maior ou igual à abertura) é codificado como `1`, enquanto um candle de baixa (preço de fechamento menor ou igual à abertura) é codificado como `0`. A estratégia escaneia a última sequência de candles concluídos e a compara com uma máscara binária selecionada pelo usuário. Quando os últimos candles coincidem exatamente com a sequência configurada, a estratégia abre uma posição na direção escolhida e imediatamente anexa tanto uma ordem de take-profit quanto uma de stop-loss expressadas em pips.

A implementação depende exclusivamente de APIs de alto nível do StockSharp: as assinaturas de candles fornecem dados, o binding cuida da entrega de eventos, e o módulo de proteção integrado gerencia as saídas. Nenhuma coleção personalizada ou acesso direto a valores de indicadores é necessário, mantendo a lógica concisa e robusta.

## Lógica do padrão
- Os candles são avaliados apenas após estarem completamente fechados (`CandleStates.Finished`).
- Cada candle se torna um dígito binário:
  - `1` – o candle é de alta ou neutro (`Close >= Open`).
  - `0` – o candle é de baixa ou neutro (`Close <= Open`). Candles doji correspondem a ambos os dígitos, exatamente como no expert original.
- A máscara é selecionada da enumeração `MorsePatternMasks`. Contém cada sequência binária de comprimento 1 a 5 que apareceu na versão MT5 (por exemplo `000`, `1011`, `11111`).
- A estratégia mantém uma janela deslizante dos candles mais recentes. Quando a janela mais nova corresponde à máscara, o sinal de entrada é disparado.

Este comportamento espelha a implementação MT5 que chamava `CopyRates` e comparava cada barra com a string do padrão caractere por caractere.

## Fluxo de trading
1. Assinar o tipo de candle configurado e aguardar até que barras suficientes sejam acumuladas para cobrir o comprimento da máscara.
2. Para cada candle concluído:
   - Atualizar as máscaras internas que classificam o candle como de alta, de baixa ou neutro.
   - Ignorar verificações adicionais até que pelo menos tantos candles tenham sido processados quanto a máscara exige.
   - Se os últimos candles coincidem exatamente com a máscara selecionada, verificar a direção desejada.
   - Enviar uma ordem de mercado na direção do sinal (`BuyMarket` ou `SellMarket`). Quando existe uma posição oposta, a estratégia primeiro a fecha aumentando o volume da ordem, reproduzindo o comportamento do consultor especialista original.
3. `StartProtection` anexa imediatamente um stop-loss e um take-profit expressados em unidades de preço. As ordens protetoras são gerenciadas pelo motor do StockSharp usando saídas de mercado para evitar llenados perdidos.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `CandleType` | Candles de 5 minutos (`TimeSpan.FromMinutes(5).TimeFrame()`) | Tipo de dados usado para construir a sequência Morse. |
| `Pattern` | `_0` (`"0"`) | Máscara binária para comparar com os candles mais recentes. Os valores vêm diretamente de `MorsePatternMasks`. |
| `Direction` | `Sides.Buy` | Se abrir uma posição comprada (`Buy`) ou vendida (`Sell`) quando o padrão aparece. |
| `TakeProfitPips` | `50` | Distância da entrada ao take-profit em pips. A estratégia se adapta automaticamente a cotações forex de 3 e 5 decimais multiplicando o passo de preço por dez. |
| `StopLossPips` | `50` | Distância da entrada ao stop-loss em pips, usando o mesmo cálculo de pips acima. |
| `Volume` (propriedade da estratégia) | definido pelo usuário | Tamanho da ordem em lotes/contratos, equivalente a `InpLot` no expert MT5. |

Todos os parâmetros suportam a janela de parâmetros do StockSharp, podem ser otimizados e podem ser alterados antes de iniciar a estratégia.

## Gestão de risco
- `StartProtection` anexa ambos os alvos usando offsets baseados em preço derivados das configurações de pips. As saídas são executadas com ordens de mercado para que o comportamento corresponda à classe de trade MT5 que definia valores de stop-loss e take-profit na entrada da posição.
- Como a estratégia não faz pirâmide, uma nova operação é ignorada enquanto existe uma posição na mesma direção. Quando o padrão aparece enquanto mantém a direção oposta, o volume é automaticamente aumentado para inverter a posição.
- O log padrão do StockSharp reporta cada entrada no diário da estratégia.

## Notas de uso
- As máscaras binárias são intencionalmente curtas (até cinco candles) para manter a lógica fiel à ideia original. Considere combinar múltiplas máscaras de padrão através da orquestração de portfólio se um vocabulário mais rico for necessário.
- A conversão de pips depende do passo de preço do instrumento. Para símbolos exóticos com incrementos não padrão, você pode ajustar `TakeProfitPips` e `StopLossPips` manualmente.
- A estratégia não filtra por hora do dia ou volatilidade. Você pode envolvê-la dentro de uma estratégia pai que lida com sessões ou indicadores adicionais se necessário.
- Ao testar, certifique-se de que a propriedade `Volume` corresponda ao tamanho de lote esperado. O testador do StockSharp reutilizará as mesmas proteções e fluxo de ordens que o modo ao vivo.

## Referência de padrões
Exemplos de valores de enumeração:
- `_0` → `"0"` (candle de baixa individual)
- `_5` → `"11"` (dois candles de alta consecutivos)
- `_20` → `"0110"` (sequência baixa-alta formando um zig-zag)
- `_33` → `"00011"` (três candles de baixa seguidos de dois de alta)
- `_61` → `"11111"` (cinco candles de alta consecutivos)

Qualquer uma das 62 máscaras pode ser selecionada no painel de parâmetros para reproduzir exatamente a assinatura de código Morse exigida pelo plano de trading.
