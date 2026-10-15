# Estratégia Spasm
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Resumo
- Conversão do assessor especialista MetaTrader 5 *Spasm (edição de barabashkakvn)* para a API de alto nível do StockSharp.
- Opera rompimentos de um canal adaptativo dimensionado pela volatilidade recente e alterna entre regimes de alta e baixa.
- Funciona em qualquer instrumento e período fornecido pelo parâmetro `CandleType`, com padrão de velas de uma hora.

## Preparação de dados
1. Subscreve a série de velas definida por `CandleType` para o instrumento da estratégia.
2. Constrói um estimador de volatilidade a partir das últimas `VolatilityPeriod` velas:
   - Quando `UseWeightedVolatility` está desabilitado, o estimador é uma média móvel simples do intervalo por vela.
   - Quando `UseWeightedVolatility` está habilitado, o estimador torna-se uma média móvel linearmente ponderada que enfatiza as barras mais recentes.
3. O intervalo por vela é `High - Low` por padrão. Se `UseOpenCloseRange` estiver habilitado, é usada a diferença absoluta entre abertura e fechamento, reproduzindo a troca de modo do EA original.
4. O intervalo médio bruto é convertido em passos de preço e multiplicado por `VolatilityMultiplier`. O resultado é truncado para um número inteiro de passos e finalmente multiplicado pelo tamanho do tick do instrumento para formar o limiar de rompimento.
5. Durante as primeiras `VolatilityPeriod * 3` velas terminadas, a estratégia coleta a máxima mais alta e a mínima mais baixa junto com seus timestamps para decidir qual oscilação é mais recente. Essa informação inicializa o estado de tendência inicial e os preços de referência uma vez que velas suficientes sejam processadas.

## Parâmetros
| Nome | Padrão | Descrição |
| --- | --- | --- |
| `Volume` | `1` | Volume da ordem aplicado a cada entrada de mercado. |
| `VolatilityMultiplier` | `5` | Multiplicador aplicado à volatilidade média para dimensionar o buffer de rompimento. |
| `VolatilityPeriod` | `24` | Número de velas usadas para a rotina de média de volatilidade e a verificação inicial de oscilações. |
| `UseWeightedVolatility` | `false` | Muda a média de volatilidade de média móvel simples para média móvel linearmente ponderada. |
| `UseOpenCloseRange` | `false` | Usa o movimento absoluto abertura-fechamento como fonte de volatilidade em vez do intervalo máxima-mínima. |
| `StopLossFraction` | `0.5` | Fração do limiar de volatilidade empregado para calcular a distância do stop-loss. Um mínimo de três passos de preço é aplicado. |
| `CandleType` | `período de 1 hora` | Tipo de vela e período usado para todos os cálculos. |

## Lógica de trading
1. **Rastreamento de tendência**
   - A estratégia mantém `_highestPrice` e `_lowestPrice` como âncoras da oscilação atual.
   - Sempre que o preço avança mais do que o limiar atual acima da máxima armazenada, `_highestPrice` é atualizado para a máxima da vela. Analogamente, uma queda além do limiar atualiza `_lowestPrice` para a mínima da vela.
   - O booleano `_isTrendUp` armazena se a estratégia está atualmente no regime de alta (true) ou de baixa (false).
2. **Regras de entrada**
   - Quando `_isTrendUp` é `false` (regime de baixa) e o fechamento da vela excede `_lowestPrice + threshold`, a estratégia muda para o modo de alta e envia `BuyMarket(Volume + Math.Abs(Position))`. Isso fecha qualquer exposição a vendido e abre uma posição comprada igual a `Volume`.
   - Quando `_isTrendUp` é `true` (regime de alta) e o fechamento da vela cai abaixo de `_highestPrice - threshold`, a estratégia muda para o modo de baixa e envia `SellMarket(Volume + Math.Abs(Position))` para reverter a uma posição vendida.
3. **Gestão de stops**
   - Ao entrar em uma posição comprada, o preço de stop é colocado em `entry - max(threshold * StopLossFraction, 3 * priceStep)`.
   - Ao entrar em uma posição vendida, o preço de stop é colocado em `entry + max(threshold * StopLossFraction, 3 * priceStep)`.
   - Se a mínima de uma vela atingir o stop comprado ou a máxima atingir o stop vendido, a posição correspondente é fechada enviando uma ordem de mercado. Os stops são desabilitados quando `StopLossFraction` é definido como zero.
4. **Controles de risco e infraestrutura**
   - `StartProtection()` é chamado durante a inicialização para que as proteções de risco integradas se tornem ativas assim que a estratégia iniciar.
   - A estratégia reage apenas a velas terminadas para evitar ruído intrabarra, espelhando o recálculo barra a barra do EA original.
   - Todos os comentários e nomes de parâmetros são mantidos em inglês conforme os requisitos.

## Diferenças da versão MQL
- O EA original recalculava limiares a cada tick. Neste porto, a lógica é executada em velas completadas porque a API de alto nível opera com subscrições de velas.
- A aplicação do stop-loss ocorre em dados de velas. Acionamentos de stop intrabarra que se revertam dentro da mesma barra são portanto avaliados nos limites da vela.
- Propriedades do símbolo como spread e níveis de stop específicos do broker não estão disponíveis na mesma forma no StockSharp. Um mínimo conservador de três passos de preço é usado quando a distância de stop calculada é muito pequena, reproduzindo o fallback da implementação MetaTrader.

## Notas de uso
- Certifique-se de que o instrumento da estratégia expõe um `PriceStep` válido. Se não for fornecido, o código define o passo como `1` por padrão.
- A estratégia é agnóstica em relação à direção e pode ser usada em instrumentos spot, futuros ou CFD, desde que o feed entregue as velas configuradas.
- Nenhum alvo de take-profit é definido; as saídas ocorrem apenas via mudanças de regime ou acionamentos de stop-loss.
