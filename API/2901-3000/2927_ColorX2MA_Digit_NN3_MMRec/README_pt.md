# Estratégia ColorX2MA Digit NN3 MMRec
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Recria o Consultor Especialista de triplo período de tempo baseado no indicador ColorX2MA Digit.
- Usa um indicador de média móvel de suavização dupla personalizado que imita a lógica X2MA original com métodos de suavização selecionáveis (Simple, Exponential, Smoothed, Linear Weighted, Jurik, Kaufman Adaptive).
- Aplica três instâncias de indicador independentes (12h, 6h, 3h por padrão); cada instância pode abrir ou fechar exposição comprada/vendida independentemente de acordo com suas próprias configurações.
- Agrega o volume desejado de cada período de tempo e negocia a diferença com ordens de mercado para que a posição líquida sempre corresponda à soma dos sinais individuais.
- Os sinais são confirmados após `SignalBars` barras consecutivas com a mesma direção de inclinação, o que emula o deslocamento `SignalBar` na versão MQL.
- Inclui interruptores opcionais para permitir ou proibir a abertura/fechamento de exposição comprada e vendida separadamente para cada período de tempo, reproduzindo as flags "Must Trade" do original.

## Parâmetros
- **A/B/C Candle Type** – tipo de dados (período de tempo) para cada instância de indicador.
- **Fast/Slow Method** – método de suavização para a primeira e segunda média móvel dentro do clone X2MA.
- **Fast/Slow Length** – período das respectivas médias móveis (padrões: 12 e 5).
- **Signal Bars** – número de barras consecutivas necessárias antes de aceitar uma nova direção (padrão: 1).
- **Digits** – precisão de arredondamento aplicada à saída do indicador antes do cálculo da inclinação (simula a entrada `Digit`).
- **Price Type** – fonte de preço usada pelo indicador (fechamento, abertura, mediana, típico, ponderado, simplificado, quarto, fórmulas TrendFollow e DeMark).
- **Allow Long/Short Entry/Exit** – flags booleanas que controlam se um período de tempo específico pode abrir ou fechar exposição comprada/vendida.
- **Volume** – volume negociado contribuído pelo período de tempo quando está comprado (positivo) ou vendido (negativo).

## Sinais e gestão de posição
1. Cada período de tempo processa apenas velas concluídas e atualiza seu valor de indicador.
2. Quando a inclinação da média duplamente suavizada se torna positiva (índice de cor 0 no indicador MQL) e permanece assim pelo número configurado de barras, o contexto torna-se de alta:
   - A exposição vendida existente é fechada se `Allow Short Exit` estiver habilitado.
   - Uma posição comprada do volume configurado é aberta se `Allow Long Entry` estiver habilitado.
3. Quando a inclinação se torna negativa (índice de cor 2), o contexto torna-se de baixa:
   - A exposição comprada existente é fechada se `Allow Long Exit` estiver habilitado.
   - Uma posição vendida do volume configurado é aberta se `Allow Short Entry` estiver habilitado.
4. A estratégia soma os volumes desejados dos três períodos de tempo e envia uma ordem de mercado pela diferença com o portfólio atual para que a `Position` global sempre reflita a intenção combinada.

## Notas
- Tipos de suavização não suportados da biblioteca MQL (JurX, Parabolic MA, T3, variações VIDYA/AMA) não estão expostos; se necessário, podem ser mapeados manualmente.
- O indicador personalizado arredonda valores usando `Digits` e funciona apenas em velas concluídas, evitando o redesenho intra-barra.
- Nenhum stop-loss ou take-profit integrado é adicionado porque o original usa gestão de dinheiro MMRec; os parâmetros `Volume` permitem o dimensionamento manual em vez disso.
