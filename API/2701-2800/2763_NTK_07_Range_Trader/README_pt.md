# Estratégia NTK 07 de Negociação em Faixa
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia NTK 07 de Negociação em Faixa é um port do consultor especialista do MetaTrader "NTK 07". O algoritmo mantém ordens stop simétricas em torno do preço de mercado atual e gerencia posições abertas com lógica de trailing e take-profit configuráveis. O objetivo é capturar rompimentos que ocorrem perto das bordas ou do centro de uma faixa de preço de curto prazo, respeitando controles de risco estritos.

## Ideias centrais

- **Gatilhos de entrada** – Quando a estratégia está sem posição, avalia uma faixa de lookback configurável. Se o preço estiver nas bordas da faixa ou próximo ao seu ponto médio (dependendo do modo de negociação selecionado), coloca tanto ordens buy stop quanto sell stop em um offset definido em passos de preço.
- **Consciência da faixa** – Os preços mais altos e mais baixos das últimas *N* velas finalizadas definem a faixa de trading. Um comprimento zero desativa o filtro e permite colocar ordens imediatamente.
- **Risco adaptativo** – Cada entrada usa o volume base enquanto um multiplicador de lote opcional pode piramidizar ordens stop adicionais após a abertura de uma posição. Um limite de volume em nível de portfólio bloqueia novas ordens quando a exposição excederia o teto.
- **Gestão de saída** – Assim que uma posição é preenchida, a ordem stop oposta é cancelada. A estratégia então registra stop protetor e ordens de take-profit opcionais usando os offsets configurados. O trailing pode seguir o máximo/mínimo da vela anterior, uma média móvel ou um buffer de distância fixa.
- **Filtro de sessão** – O trading é permitido apenas entre as horas de início e fim selecionadas e é automaticamente desativado nos fins de semana.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| **Entry Volume** | Tamanho base para cada ordem de entrada. |
| **Total Volume Limit** | Tamanho máximo de posição acumulada. Um valor de `0` desativa o teto. |
| **Net Step** | Distância em passos de preço entre o mercado e as ordens stop de entrada. |
| **Stop Loss** | Offset inicial do stop-loss em passos de preço relativo ao preço de entrada. |
| **Take Profit** | Distância do take-profit em passos de preço. Definir como `0` para desativar alvos de lucro. |
| **Trailing Stop** | Distância em passos de preço usada para a lógica de trailing. |
| **Lot Multiplier** | Multiplicador aplicado ao piramidizar em uma posição existente. |
| **Trail High/Low** | Se habilitado, os stops protetores seguem os extremos da vela anterior. |
| **Trail Moving Average** | Habilita o trailing usando um valor de média móvel. Apenas um modo de trailing pode estar ativo. |
| **Trading Start/End Hour** | Janela de tempo de plataforma inclusiva para o trading. |
| **Range Bars** | Número de velas concluídas usadas para calcular a faixa de trading. `0` desativa o filtro. |
| **Trade Mode** | `EdgesOfRange` exige que o preço toque as bordas da faixa, `CenterOfRange` aguarda até que o preço esteja próximo do ponto médio da faixa. |
| **MA Period** | Comprimento da média móvel usada para trailing. |
| **Candle Type** | Agregação de velas usada para todos os cálculos. |

## Fluxo de trabalho

1. **Assinatura de dados** – A estratégia assina a série de velas configurada e calcula a média móvel, bem como o preço mais alto e mais baixo ao longo do comprimento de faixa escolhido.
2. **Estado sem posição** – Enquanto nenhuma posição está aberta, a estratégia avalia a condição da faixa. Se satisfeita, coloca ordens buy stop e sell stop pareadas no offset especificado, respeitando o limite de volume global.
3. **Gerenciamento de posição** – Quando uma entrada é preenchida, o stop oposto é cancelado. A estratégia imediatamente coloca ordens de stop-loss protetor e take-profit opcional. A lógica de trailing então atualiza o stop protetor em cada nova vela finalizada.
4. **Piramidização** – Se o multiplicador de lote for maior que `1`, uma ordem stop adicional é colocada na direção da posição atual enquanto o limite de volume total permitir.
5. **Saída** – Stops ou take-profits nivelam a posição e cancelam as ordens protetoras restantes. O sistema então volta a monitorar a próxima interação da faixa.

## Notas

- A estratégia funciona inteiramente com passos de preço, tornando-a adequada para instrumentos com diferentes tamanhos de tick.
- O trading é automaticamente desativado aos sábados e domingos para espelhar o comportamento da implementação MQL original.
- Apenas um modo de trailing pode ser habilitado por vez; habilitar ambos acionará um erro de configuração na inicialização.
