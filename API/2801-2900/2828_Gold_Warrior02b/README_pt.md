# Estratégia GoldWarrior02b
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia algorítmica convertida do expert advisor MetaTrader *GoldWarrior02b*.
Combina um medidor de impulso, o Commodity Channel Index (CCI) e um simples detector de oscilação ZigZag
para operar próximo ao final de cada bloco de 15 minutos.

A implementação tem como alvo a API de alto nível do StockSharp e foca em posições líquidas.
O hedging multinível do script original não é suportado porque o StockSharp trabalha com posições neteadas.

## Conceito

- Usar um indicador de impulso personalizado que faz a média da diferença entre os preços de abertura e fechamento das velas.
- Avaliar os valores do CCI para detectar reversões de sobrecompra/sobrevenda e picos de momentum fortes.
- Derivar uma direção de oscilação ZigZag de máximas e mínimas recentes para evitar operar contra o movimento dominante.
- Avaliar sinais apenas durante os segundos finais (>= 45s) dos minutos 14, 29, 44 e 59.
- Aplicar gerenciamento de risco dinâmico com stop-loss, take-profit, trailing-stop e um objetivo de lucro global.

## Regras de Entrada

Uma operação é considerada apenas se não há posição atualmente aberta e a vela atual fecha dentro da
janela de tempo descrita acima.

### Configuração Comprada
- A oscilação ZigZag aponta para baixo (a mínima recente é menor que a anterior).
- Qualquer um dos seguintes:
  - CCI sobe acima de sua leitura anterior enquanto o CCI anterior estava abaixo de -50, CCI atual abaixo de -30,
    impulso torna-se positivo e o impulso anterior era negativo.
  - Ou CCI cai abaixo de -200, o CCI anterior era ainda mais baixo, impulso permanece abaixo do limiar positivo
    e o impulso anterior é mais fraco que o valor atual.

### Configuração Vendida
- A oscilação ZigZag aponta para cima (a máxima recente é maior que a anterior).
- Qualquer um dos seguintes:
  - CCI cai abaixo de sua leitura anterior enquanto o CCI anterior estava acima de 50, CCI atual acima de 30,
    impulso torna-se negativo e o impulso anterior era positivo.
  - Ou CCI ultrapassa 200, o CCI anterior era mais alto, impulso permanece acima do limiar negativo
    e o impulso anterior é mais forte que o valor atual.

Se o impulso anterior permanece entre os limiares de compra e venda configurados, os sinais são ignorados.

## Regras de Saída

- **Stop-loss**: fecha a posição quando o preço cruza a distância de stop a partir do preço de entrada.
- **Take-profit**: fecha após atingir a distância de lucro configurada.
- **Trailing stop**: assim que o preço avança por `(TrailingStop + TrailingStep)` pontos, o nível de trailing segue o preço
  a uma distância de `TrailingStop` pontos. Cruzar o nível de trailing sai da operação.
- **Objetivo de lucro global**: fecha a posição quando o PnL não realizado excede o valor especificado (em moeda da conta).

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `BaseVolume` | Tamanho da operação para entradas. | `0.1` |
| `StopLossPoints` | Distância do stop em pontos. | `100` |
| `TakeProfitPoints` | Distância do take-profit em pontos. | `150` |
| `TrailingStopPoints` | Distância base do trailing stop. | `5` |
| `TrailingStepPoints` | Distância adicional antes de o trailing stop ativar. | `5` |
| `ImpulsePeriod` | Período para cálculos de CCI e impulso. | `21` |
| `ZigZagDepth` | Mínimo de barras entre novas oscilações ZigZag. | `12` |
| `ZigZagDeviation` | Movimento mínimo de preço (em pontos) para confirmar uma oscilação. | `5` |
| `ZigZagBackstep` | Mínimo de barras antes de aceitar uma nova oscilação. | `3` |
| `ProfitTarget` | Limiar de lucro não realizado para fechar todas as posições. | `300` |
| `ImpulseSellThreshold` | Limiar de impulso para vendidos (tipicamente negativo). | `-30` |
| `ImpulseBuyThreshold` | Limiar de impulso para comprados (tipicamente positivo). | `30` |
| `CandleType` | Período usado para cálculos. | `Período de 5 minutos` |

## Notas

- O indicador de impulso é uma média móvel da diferença entre os valores de abertura e fechamento das velas
  dimensionada pelo passo de preço do instrumento.
- Os cálculos de trailing e PnL dependem do `PriceStep` e `StepPrice` do instrumento para converter
  distâncias em pontos para moeda da conta.
- O expert advisor original dimensiona tamanhos de posição e implanta camadas de hedging.
  Esta portagem do StockSharp mantém uma única posição líquida por instrumento, correspondendo com o modelo de execução do StockSharp.
- Para replicar o comportamento original mais de perto, considere habilitar uma assinatura de velas de 15 minutos
  e garantir que a latência de dados de tick permita a execução logo após o timestamp de fechamento.

## Aviso Legal

Esta amostra é para fins educacionais. Antes de executar em mercados ao vivo, valide a estratégia sob
condições realistas de dados, latência e comissões.
