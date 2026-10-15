# Estratégia de Explosion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia reproduz a lógica do especialista MetaTrader "Explosion". Ela observa o intervalo de cada vela concluída e entra no mercado quando a última barra "explode" – sua altura mais do que dobra o intervalo da barra anterior. A direção é decidida pelo corpo da vela: um corpo altista abre uma posição comprada, enquanto um corpo baixista abre uma posição vendida.

## Regras de trading

1. Processa apenas velas concluídas provenientes da assinatura configurada `CandleType`.
2. Calcula o intervalo atual como `High - Low` e o compara com o intervalo da vela anterior.
3. Uma entrada **comprada** é aberta quando `currentRange > previousRange * 2` e o fechamento está acima da abertura.
4. Uma entrada **vendida** é aberta quando `currentRange > previousRange * 2` e o fechamento está abaixo da abertura.
5. Quando `OnlyOnePositionPerBar` está habilitado, no máximo um trade por direção é permitido para um tempo de abertura de vela. Tentativas na mesma barra são ignoradas.
6. A estratégia mantém uma única posição líquida, portanto um trade oposto fecha automaticamente qualquer exposição existente antes de estabelecer a nova direção.
7. Mecânicas de proteção:
   - `StopLossPips` e `TakeProfitPips` colocam níveis de saída virtuais medidos em pips a partir do preço de entrada.
   - `TrailingStopPips` e `TrailingStepPips` movem o stop quando o preço viaja a favor da posição pelo menos a distância de trailing e a cada passo adicional.
   - O multiplicador de pip opcional emula o assistente de auto-dígitos MQL multiplicando o tamanho do pip por 10 em instrumentos de 3 e 5 dígitos.

## Parâmetros

| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `TradeVolume` | `0.01` | Volume de ordem de mercado usado nas entradas. |
| `StopLossPips` | `20` | Distância de stop-loss em pips. Zero desativa o stop. |
| `TakeProfitPips` | `10` | Distância de take-profit em pips. Zero desativa o take. |
| `TrailingStopPips` | `25` | Distância de ativação para o trailing stop em pips. Zero desativa o trailing. |
| `TrailingStepPips` | `5` | Movimento adicional em pips necessário antes que o trailing stop avance. Deve permanecer positivo quando o trailing está habilitado. |
| `UseAutoPipMultiplier` | `true` | Multiplica o tamanho do pip por 10 em instrumentos com 3 ou 5 casas decimais, correspondendo ao assistente de auto-dígitos MQL. |
| `OnlyOnePositionPerBar` | `true` | Proíbe mais de uma entrada por tempo de abertura de barra. |
| `CandleType` | `TimeSpan.FromMinutes(1).TimeFrame()` | Série de velas usada para os cálculos. |

## Notas sobre a conversão

- O StockSharp trabalha com uma posição líquida, portanto o hedging de múltiplas ordens simultâneas do Expert Advisor original não é suportado. O comportamento é equivalente a habilitar `OnlyOneOpenedPos` na versão MQL.
- As atualizações do trailing stop são realizadas nos fechamentos de velas em vez de a cada tick. A lógica corresponde aos limiares originais enquanto permanece compatível com a API de alto nível.
- O multiplicador de pip reproduz a detecção automática de dígitos que escala as distâncias por 10 em símbolos forex de 5 dígitos.

## Uso sugerido

1. Escolha o instrumento e o período que correspondam ao especialista original (por exemplo, os gráficos M15/M30 recomendados para pares forex).
2. Ajuste os parâmetros de risco baseados em pips à volatilidade do instrumento.
3. Habilite o registro para monitorar quando o trailing stop avança e como os níveis de proteção são recalculados.
