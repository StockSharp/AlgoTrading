# Estratégia VR Overturn
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Recria o expert MetaTrader «VR---Overturn» utilizando as APIs de alto nível do StockSharp.
- Mantém apenas uma posição aberta por vez e avalia imediatamente a próxima operação assim que a anterior é fechada.
- Desenvolvido para traders discricionários que desejam reversão automática de posição com dimensionamento martingale ou anti-martingale.

## Lógica de negociação
1. **Posição inicial** – a estratégia abre a primeira operação na direção configurada (`FirstPositionDirection`) com o volume base (`BaseVolume`).
2. **Stop loss / take profit** – ordens de saída protetoras são anexadas automaticamente usando `StopLossPips` e `TakeProfitPips`. O motor converte pips em deslocamentos de preço absolutos analisando o passo de preço do instrumento (instrumentos de 3 e 5 dígitos recebem o ajuste ×10, assim como no expert original).
3. **Processamento do fechamento de posição** – quando uma posição é fechada por qualquer ordem protetora, a estratégia registra:
   - Lado da operação fechada (comprado ou vendido).
   - Volume executado.
   - PnL realizado (diferença entre preço de entrada e de saída).
4. **Dimensionamento da próxima entrada** – o resultado armazenado decide o lado e o tamanho do lote da próxima ordem.
   - Operações vencedoras mantêm a mesma direção, operações perdedoras invertem a direção.
   - O modo martingale multiplica o tamanho da posição após uma perda e o redefine para o volume base após um ganho.
   - O modo anti-martingale multiplica o tamanho da posição após um ganho e o redefine para o volume base após uma perda.
5. **Arredondamento de lote** – o tamanho calculado é ajustado para o passo de volume mais próximo do instrumento antes de enviar uma ordem a mercado.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-----------|--------|
| `FirstPositionDirection` | Direção da primeira operação (Buy/Sell). | Buy |
| `Mode` | Regime de dimensionamento: Martingale (aumento após perdas) ou AntiMartingale (aumento após ganhos). | Martingale |
| `BaseVolume` | Volume inicial da posição. Usado quando uma sequência é reiniciada. | 0.1 |
| `StopLossPips` | Distância ao stop loss em pips. | 30 |
| `TakeProfitPips` | Distância ao take profit em pips. | 90 |
| `LotMultiplier` | Multiplicador aplicado durante o passo de expansão (após perda para martingale, após ganho para anti-martingale). | 1.6 |

## Gestão de riscos
- Utiliza `StartProtection` para anexar ordens de stop-loss e take-profit em cada entrada.
- As distâncias de stop e alvo são deslocamentos de preço absolutos derivados dos valores de pip configurados.
- Nenhuma lógica de trailing adicional é aplicada, portanto o risco é inteiramente controlado pelas ordens protetoras e pelas regras de reversão de posição.

## Notas operacionais
- A estratégia não depende de velas nem indicadores; reage puramente a confirmações de operações (`OnOwnTradeReceived`).
- Se uma ordem protetora for parcialmente executada, a estratégia acumula o valor restante até que a posição esteja zerada antes de agir novamente.
- Valores de comissão e swap não estão disponíveis nas operações do StockSharp, portanto a comparação de lucro usa apenas a diferença de preço. Considere ampliar stops ou multiplicadores se sua corretora cobrar taxas significativas.
- Funciona com qualquer instrumento que forneça metadados de passo de preço e volume; verifique ambos antes de implantar em produção.
