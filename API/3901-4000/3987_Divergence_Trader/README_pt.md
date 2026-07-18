# Trader de Divergência (Conversão Clássica)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia reproduz o comportamento do MetaTrader 4 consultor especialista **Divergence Trader** dentro do StockSharp alto nível API. Duas médias móveis simples são calculadas sobre o preço da vela selecionada (aberta por padrão). O sistema monitora como a distância entre as médias rápida e lenta muda de uma barra para outra:

* Quando o spread aumenta para cima e o valor da divergência permanece entre o *Limiar de Compra* e o *Limiar de Ficar Fora*, uma posição longa é aberta ou uma posição curta existente é coberta.
* Quando o spread aumenta para baixo dentro dos limites espelhados, uma posição curta é inserida ou uma negociação longa existente é fechada.

Apenas velas completas são usadas, correspondendo ao processamento barra por barra do consultor especialista original. Todas as regras de gerenciamento são implementadas com chamadas de alto nível orientadas a eventos (`BuyMarket` / `SellMarket`).

## Regras de negociação

1. Assine o tipo de vela configurado e calcule dois SMAs com períodos *Rápido SMA* e *Lento SMA*.
2. Calcule o spread atual (`fast - slow`) e compare-o com o spread anterior para obter o valor da divergência.
3. Insira comprado se a divergência for positiva, maior ou igual a *Limiar de Compra* e menor ou igual a *Limiar de Fique Fora*.
4. Insira short se a divergência for negativa, menor ou igual a `-Buy Threshold` e maior ou igual a `-Stay Out Threshold`.
5. Inverta uma posição existente sempre que aparecer um sinal oposto.
6. Restrinja novas entradas à janela de horário local entre *Start Hour* e *Stop Hour* (é possível passar da meia-noite).

## Gestão de risco

* Os níveis fixos opcionais de *Take Profit (pips)* e *Stop Loss (pips)* são monitorados nas máximas/mínimas das velas.
* O *Break-Even Trigger (pips)* move o stop para `entry ± Break-Even Buffer` assim que a posição ganha o número especificado de pips.
* O *Trailing Stop (pips)* segue o preço mais favorável quando a negociação gera lucro. A configuração 9999 desativa o trailing stop, espelhando o padrão EA original.
* O gerenciamento da cesta fecha todas as exposições abertas quando o P&L não realizado atinge *Basket Profit* ou cai abaixo de `-Basket Loss` na moeda da conta.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `Order Volume` | Volume usado quando uma nova posição é aberta. |
| `Fast SMA` / `Slow SMA` | Períodos para as duas médias móveis simples. |
| `Applied Price` | Componente de vela encaminhado para ambas as médias móveis. |
| `Buy Threshold` | Limite de divergência inferior que permite negociações longas. |
| `Stay Out Threshold` | Limite superior de divergência acima do qual nenhuma nova negociação é realizada. |
| `Take Profit (pips)` / `Stop Loss (pips)` | Saídas rígidas opcionais medidas em pips. |
| `Trailing Stop (pips)` | Distância final aplicada depois que a negociação se torna lucrativa. |
| `Break-Even Trigger (pips)` | Lucro em pips necessário antes de mover o stop para o ponto de equilíbrio. |
| `Break-Even Buffer (pips)` | Buffer adicional adicionado ao ponto de equilíbrio. |
| `Basket Profit` / `Basket Loss` | Limites de patrimônio global na moeda da conta. |
| `Start Hour` / `Stop Hour` | Janela da sessão de negociação local. |
| `Candle Type` | Prazo usado para assinatura e cálculos de velas. |

## Notas de uso

* Anexe a estratégia a um título e defina o tipo de vela que corresponda ao período original do gráfico.
* Certifique-se de que as propriedades `PriceStep`/`StepPrice` do instrumento estejam configuradas para que os controles baseados em pip funcionem corretamente.
* Para desabilitar recursos como trailing stop ou mudança de ponto de equilíbrio, mantenha seus parâmetros no valor sentinela legado (9999) ou zero.
