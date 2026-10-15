# Estratégia de Grade Amstell
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia de Grade Amstell é um port em C# do consultor especialista MetaTrader 5 `exp_Amstell.mq5`. Ela cria uma grade simétrica de compra/venda e aplica um take profit virtual a entradas individuais. A conversão segue as diretrizes da API de alto nível do StockSharp e substitui o processamento de ticks pelo processamento de candles enquanto mantém a ideia original intacta.

## Como funciona

1. **Inicialização**
   - A estratégia assina o tipo de candle configurado e inicia a proteção de posição.
   - Um tamanho de pip ajustado é calculado a partir do `PriceStep` do ativo e da precisão decimal. Símbolos de cinco dígitos e três dígitos recebem automaticamente um multiplicador de 10x, espelhando a implementação MT5.

2. **Primeira negociação**
   - Quando os preços de compra e venda registrados estão vazios (lançamento inicial), uma ordem de compra a mercado é enviada imediatamente. Isso inicializa a grade exatamente como o consultor especialista original.

3. **Expansão da grade**
   - Uma nova ordem de **compra** é emitida sempre que o preço de fechamento atual está pelo menos `StepPips` abaixo do último preço de compra registrado.
   - Uma nova ordem de **venda** é emitida sempre que o preço está pelo menos `StepPips` acima do último preço de venda registrado.
   - A estratégia rastreia internamente pilhas separadas de comprados e vendidos para que ordens alternadas possam coexistir mesmo em uma conta de netting. Ordens opostas primeiro reduzem a outra pilha antes de adicionar nova exposição, reproduzindo o comportamento de hedge da versão MT5.

4. **Take Profit virtual**
   - Cada posição comprada aberta é monitorada de forma independente. Quando o preço avança `TakeProfitPips`, uma venda a mercado é enviada apenas pelo volume dessa posição.
   - Cada posição vendida aberta é tratada de forma similar na direção oposta. O take profit é "virtual" porque as posições são fechadas programaticamente sem usar ordens TP do lado do broker.
   - Após uma direção ter sido totalmente fechada enquanto o lado oposto ainda existe, o preço do último negócio correspondente é limpo para que a próxima ordem nessa direção possa disparar imediatamente, assim como no código original.

5. **Rastreamento de estado**
   - O manipulador `OnOwnTradeReceived` reconstrói as pilhas de comprados/vendidos a partir de negociações executadas, permitindo que preenchimentos parciais e reversões sejam tratados com elegância.
   - Os últimos preços de compra/venda permanecem em cache quando ambos os lados estão zerados para que a grade aguarde o passo necessário antes de re-entrar após um reset completo.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|--------|-----------|
| `Volume` | `0.1` | Tamanho da ordem usado para cada ordem a mercado em ambas as direções. |
| `TakeProfitPips` | `50` | Distância em pips que deve ser ganha antes que uma posição individual seja fechada. |
| `StepPips` | `15` | Lacuna em pips entre ordens de grade consecutivas da mesma direção. |
| `CandleType` | `1 Minute` | Fonte de dados de candles usada para aproximar a lógica baseada em ticks. |

Todas as configurações baseadas em pips respeitam o passo de preço e a precisão do instrumento. Por exemplo, no EURUSD (5 dígitos) `StepPips = 15` corresponde a 0,0015.

## Notas práticas

- A estratégia usa preços de fechamento de candles para emular as comparações em nível de tick encontradas no código MT5. Para operações de alta frequência, diminua o período.
- Não existe stop-loss por padrão. Como em qualquer abordagem de grade, tendências descontroladas podem acumular grande exposição. Use volumes conservadores e considere supervisão baseada em sessões.
- Como os take profits são tratados virtualmente, as negociações fechadas são imediatamente refletidas no PnL da estratégia sem colocar ordens TP visíveis no broker.
- A implementação deixa os últimos preços em cache inalterados após ambos os lados serem zerados. Isso preserva o comportamento original onde a grade aguarda o deslocamento de preço antes de reiniciar.

## Arquivos

- `CS/AmstellGridStrategy.cs` – Implementação da estratégia StockSharp com extensos comentários em linha.
- `README.md`, `README_ru.md`, `README_zh.md` – Documentação completa em inglês, russo e chinês.

Este port está pronto para personalização adicional (por exemplo, gerenciamento de dinheiro, limites de risco) diretamente dentro do ecossistema StockSharp.
