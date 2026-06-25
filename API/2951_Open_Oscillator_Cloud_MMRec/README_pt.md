# Estratégia Open Oscillator Cloud MMRec
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o assessor especializado do MetaTrader **Exp_Open_Oscillator_Cloud_MMRec** para a API de alto nível do StockSharp. O sistema opera o cruzamento do indicador Open Oscillator Cloud, que compara o preço de abertura atual com as aberturas das barras mais altas e mais baixas dentro de uma janela deslizante e suaviza o resultado com uma média móvel configurável.

## Lógica da estratégia

### Construção do indicador
- Uma janela de lookback (`Oscillator Period`, padrão 20 barras) de velas concluídas do período selecionado é construída.
- A barra com o máximo mais alto é encontrada e seu preço de abertura é armazenado; a barra com o mínimo mais baixo é encontrada e seu preço de abertura é armazenado.
- Dois valores brutos são calculados para a vela atual:
  - **Banda superior** = abertura atual − preço de abertura no máximo mais alto.
  - **Banda inferior** = preço de abertura no mínimo mais baixo − abertura atual.
- Ambas as séries são suavizadas com a média móvel escolhida (`Smoothing Method`, `Smoothing Length`). Os tipos suportados são médias móveis Simples, Exponencial, Suavizada e Ponderada.
- O histórico suavizado é armazenado e o sinal é atrasado por `Signal Bar` velas completamente fechadas (padrão 1) para imitar a lógica original do EA que age na barra anterior.

### Critérios de entrada
- **Entrada comprado**: a banda superior da barra anterior estava acima da banda inferior e o último valor atrasado cruza para baixo (`upper ≤ lower`). Pode ser desativado via `Enable Long Entries`.
- **Entrada vendido**: a banda superior da barra anterior estava abaixo da banda inferior e o último valor atrasado cruza para cima (`upper ≥ lower`). Pode ser desativado via `Enable Short Entries`.

### Critérios de saída
- **Saída comprado**: a banda superior da barra anterior estava abaixo da banda inferior, sinalizando um regime baixista. Controlado por `Enable Long Exits`.
- **Saída vendido**: a banda superior da barra anterior estava acima da banda inferior, sinalizando um regime altista. Controlado por `Enable Short Exits`.
- **Gestão de risco**: se `Stop Loss Points` ou `Take Profit Points` forem maiores que zero, a estratégia fecha automaticamente a posição quando o preço atinge essas distâncias (medidas em passos de preço do instrumento) da entrada.

### Gerenciamento de ordens
- Apenas ordens de mercado são usadas. Antes de abrir uma nova posição, o lado oposto é zerado para permanecer alinhado com o comportamento de posição única do robô MetaTrader.
- O parâmetro `Trade Volume` define o tamanho base de posição para cada entrada.

## Parâmetros
- `Candle Type` – período das velas usadas para o oscilador (padrão 1 hora).
- `Oscillator Period` – número de velas na janela deslizante (padrão 20).
- `Smoothing Method` – média móvel aplicada às lacunas de abertura (Simple, Exponential, Smoothed, Weighted).
- `Smoothing Length` – comprimento da média móvel de suavização (padrão 10).
- `Signal Bar` – número de barras completamente fechadas para atrasar a avaliação do sinal (padrão 1).
- `Enable Long Entries` / `Enable Short Entries` – permite ou bloqueia a abertura de operações em cada direção.
- `Enable Long Exits` / `Enable Short Exits` – permite ou bloqueia saídas automáticas para a respectiva direção.
- `Trade Volume` – tamanho de cada ordem de mercado (padrão 1 contrato/lote).
- `Stop Loss Points` – distância do stop protetor em passos de preço (0 desativa o stop, padrão 1000).
- `Take Profit Points` – distância do alvo de lucro em passos de preço (0 desativa o alvo, padrão 2000).

## Notas de implementação
- Os métodos de suavização correspondem às opções mais comuns do EA original. Modos exóticos como JJMA, T3, VIDYA ou AMA não são portados porque o StockSharp já fornece alternativas ricas para otimização e robustez.
- Os sinais são avaliados apenas em eventos `CandleStates.Finished` para evitar agir sobre dados incompletos.
- A estratégia mantém um histórico interno de valores suavizados em vez de consultar buffers de indicadores, o que está alinhado com o fluxo de trabalho de alto nível recomendado pelo StockSharp.
- Os níveis de proteção são limpos automaticamente quando a posição fica zerada para evitar que stops desatualizados reabram operações.

## Comportamento padrão
- Seguidor de tendência em ambas as direções com confirmação atrasada para reduzir ruído.
- Usa gerenciamento de dinheiro fixo (constante `Trade Volume`) respeitando distâncias de stop loss e take profit semelhantes à versão MetaTrader.
- Adequado como modelo para experimentar com diferentes tipos de suavização ou combinar o oscilador com filtros adicionais.
