# Estratégia FX-CHAOS Scalp MT4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia FX-CHAOS Scalp MT4 é uma porta direta do consultor especialista MetaTrader 4 que combina um filtro Awesome Oscillator com níveis ZigZag construídos em fractais. A versão StockSharp mantém o design multiperíodo do sistema original: velas horárias geram sinais de negociação, enquanto velas diárias fornecem uma tendência de período maior. Dois rastreadores incorporados reconstroem o indicador "ZigZag on Fractals" examinando padrões de cinco velas e registrando altos e baixos alternados.

## Fluxo de trabalho de negociação
1. **Coleta de dados**
   - As velas horárias alimentam a lógica de execução primária e os controles de risco.
   - As velas diárias atualizam o balanço ZigZag de longo prazo usado como filtro de tendência.
   - O Awesome Oscillator (5, 34) é avaliado na série horária através do indicador de alto nível API.
2. **Reconstrução em ZigZag**
   - Cada vela acabada é armazenada em uma janela deslizante de cinco elementos.
   - Quando a vela do meio forma um fractal ascendente, o rastreador salva a vela alta como a última oscilação e muda a direção para “cima”; um fractal descendente faz o mesmo com os mínimos.
   - As oscilações consecutivas na mesma direção só serão substituídas se o novo extremo for mais pronunciado, imitando a lógica do buffer do indicador MT4.
3. **Detecção de sinal**
   - O buffer de breakout adiciona duas compensações de etapas de preço à máxima/mínima da hora anterior, espelhando o preenchimento `2*Point` encontrado no código original.
   - Para entradas longas, a vela deve abrir abaixo da máxima protegida, fechar acima dela, permanecer abaixo da oscilação horária mais recente do ZigZag, fechar acima da última oscilação diária e manter o Awesome Oscillator negativo.
   - As entradas curtas refletem as condições usando o nível de ZigZag inferior e superior em buffer e os valores do oscilador positivo.
4. **Execução de pedidos e resolução de conflitos**
   - As posições opostas são fechadas antes que uma nova ordem seja enviada, de modo que a estratégia nunca mantém negociações longas e curtas simultâneas.
   - O preço de fechamento executado é armazenado para derivar as distâncias de stop-loss e take-profit nas velas subsequentes.

## Gestão de risco
- Os limites de stop-loss e take-profit são opcionais; um valor de `0` desativa a regra correspondente.
- Ao final de cada vela finalizada, a estratégia verifica se o intervalo da vela tocou o stop ou alvo configurado e fecha a posição caso o nível tenha sido violado.
- Quando aparece um rompimento oposto, a posição é liquidada primeiro e, em seguida, a nova negociação é enviada na mesma vela para preservar a regra da posição única.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `Volume` | Volume de negociação em lotes aplicado a cada ordem de mercado. |
| `Stop Loss (pts)` | Distância em pontos para a parada de proteção. Multiplicado pela etapa do preço do título. Defina como `0` para desativar. |
| `Take Profit (pts)` | Distância em pontos para a meta de lucro. Multiplicado pela etapa de preço. Defina como `0` para desativar. |
| `Breakout Buffer` | Pontos adicionais adicionados ao extremo anterior da vela antes de testar os rompimentos. O valor padrão reproduz a almofada `2*Point` usada no MT4. |
| `Spread (pts)` | Spread médio em pontos que é adicionado ao limite de rompimento nos sinais de compra para que a entrada espelhe `2*Point + spread` do MT4. |
| `Trading Candle` | Período principal usado para entradas (o padrão é uma hora). |
| `Daily Candle` | Período de tempo maior usado para o filtro ZigZag (o padrão é um dia). |

## Notas de implementação
- A estratégia conta com `SubscribeCandles` API e `BindEx` de alto nível para evitar trabalhar diretamente com buffers de indicadores, respeitando as diretrizes do repositório.
- A etapa de preço recuperada de `Security.PriceStep` é usada para converter valores de parâmetros expressos em pontos em distâncias de preço absolutas. Se o instrumento não tiver uma etapa, o código volta para `1`.
- Ambos os rastreadores ZigZag são redefinidos em `OnReseted` e pausam a negociação até acumularem velas suficientes para determinar a primeira oscilação. Isto evita entradas prematuras quando falta contexto histórico.
- A renderização do gráfico desenha as velas horárias, o Awesome Oscillator e as negociações de estratégia para ajudar a comparar a implementação StockSharp com o modelo MT4.
