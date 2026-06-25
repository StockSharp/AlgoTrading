# Estratégia de Cruzamento de Canal de Triple MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
A **Estratégia de Cruzamento de Canal de Triple MA** negocia rompimentos direcionais quando uma média móvel rápida se move
através de uma média móvel intermediária e uma lenta. Um canal de preço no estilo Donchian é usado para gerenciar saídas e
fornecer níveis opcionais automáticos de stop-loss e take-profit. A conversão é baseada no "3MACross EA" original do MetaTrader e
mantém sua estrutura de média móvel configurável, controles de risco e lógica de trailing.

A estratégia escala até um número configurável de posições, suporta alvos de risco manuais baseados em pips e pode seguir o canal
para saídas adaptativas. Quando habilitado, o gatilho de break-even empurra o stop loss para o preço de entrada mais um buffer de
segurança.

## Lógica de Trading
- **Critérios de entrada**
  - *Comprado:* a média móvel rápida cruza acima de ambas as médias intermediária e lenta. Se `Trade On Close` estiver habilitado,
    o cruzamento deve ocorrer em uma vela completamente fechada; caso contrário, o sinal de compra é permitido enquanto a média
    rápida permanecer acima de ambas as médias mais lentas.
  - *Vendido:* a média móvel rápida cruza abaixo das médias intermediária e lenta com a mesma lógica de confirmação.
  - As posições existentes no lado oposto são fechadas e revertidas imediatamente. O escalonamento na mesma direção é permitido
    até que `Max Positions` seja atingido.
- **Critérios de saída**
  - Preço atingindo o take-profit configurado ou o alvo baseado em canal.
  - Preço tocando o nível de stop dinâmico (distância manual, trailing stop, movimento de break-even ou stop baseado em canal).
  - O trailing stop opcional ajusta depois que o preço se move a favor por pelo menos a distância do passo de trailing.

## Gestão de Risco
- Stops e alvos podem ser definidos manualmente em pips ou derivados do canal de preço quando `Auto SL/TP` está habilitado.
- A lógica de trailing stop e break-even refletem o consultor especializado original. O stop se move apenas na direção favorável e
  nunca é relaxado.
- O canal Donchian fornece limites naturais de suporte/resistência que podem ser usados para colocação automática de stop-loss e
  take-profit.
- `Max Positions` limita o número de passos de escalonamento, prevenindo piramidação descontrolada.

## Parâmetros Chave
| Parâmetro | Descrição |
|-----------|-----------|
| `Volume` | Tamanho de ordem para cada passo de escalonamento. |
| `Stop Loss (pips)` | Distância fixa para o stop protetor. Definir como `0` para desabilitar. |
| `Take Profit (pips)` | Distância fixa para o alvo de lucro. Definir como `0` para desabilitar. |
| `Trailing Stop (pips)` | Distância usada pelo trailing stop. `0` desabilita o trailing. |
| `Trailing Step (pips)` | Avanço mínimo necessário antes de atualizar o trailing stop. |
| `Break Even (pips)` | Lucro necessário antes de fixar um stop de break-even. |
| `Auto SL/TP` | Usar o canal Donchian em vez de distâncias fixas para colocação de stop-loss e take-profit. |
| `Trade On Close` | Exigir que os cruzamentos sejam confirmados em uma vela fechada. Se desabilitado, o alinhamento das médias é verificado a cada barra. |
| `Max Positions` | Número máximo de passos de escalonamento por direção. |
| `Fast/Middle/Slow MA Period` | Comprimento das médias móveis. |
| `Fast/Middle/Slow MA Shift` | Deslocamento opcional (em barras) aplicado a cada média móvel. |
| `Fast/Middle/Slow MA Type` | Modo de cálculo da média móvel (Simples, Exponencial, Suavizada, Ponderada). |
| `Channel Period` | Lookback para o máximo/mínimo do canal Donchian. |
| `Candle Type` | Período das velas processadas pela estratégia. |

## Notas de Implementação
- As distâncias em pips são convertidas usando `Security.PriceStep`. Para instrumentos sem tamanho de tick válido, a estratégia
  recorre a uma distância de `1` unidade de preço por pip.
- O gerenciamento automático de canal mantém os níveis de stop-loss e take-profit se movendo apenas mais próximos do preço atual;
  eles nunca são ampliados.
- A ativação de break-even reutiliza o passo de trailing como um buffer adicional, correspondendo ao comportamento original do EA.
- A estratégia é projetada para uso com as APIs de alto nível do StockSharp e lida com a renderização de gráficos (MAs e canal
  Donchian) para análise visual.
- Certifique-se de que a profundidade de dados históricos seja suficiente para a média móvel lenta e o período do canal para que
  os sinais de cruzamento sejam válidos.

## Uso
1. Anexar a estratégia a um instrumento e definir o período de velas desejado.
2. Configurar os períodos/métodos das médias móveis para corresponder ao EA original ou sua adaptação.
3. Escolher entre configurações de risco manuais baseadas em pips ou habilitar saídas automáticas de canal.
4. Iniciar a estratégia; ela se subscreverá às velas configuradas, calculará indicadores e negociará quando as condições de
   cruzamento forem atendidas.
5. Monitorar o trailing stop e os ajustes de break-even através dos logs e sobreposições de gráfico.

> **Aviso:** O trading automatizado envolve riscos significativos. Teste a estratégia completamente com dados históricos e em um
> ambiente de simulação antes de implantar em mercados ao vivo.
