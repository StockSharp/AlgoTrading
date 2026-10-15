# Estratégia Color XMUV com Filtro de Tempo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia porta o consultor especialista MetaTrader **Exp_ColorXMUV_Tm** para o StockSharp. Recria a linha suavizada Color XMUV original e o filtro de janela de tempo enquanto usa a API de negociação de alto nível do StockSharp. A estratégia segue a cor da linha suavizada: uma transição para azul-esverdeado (subindo) aciona o gerenciamento de comprados, enquanto uma transição para magenta (caindo) impulsiona o gerenciamento de vendidos.

## Lógica central
- Para cada candle finalizado a estratégia constrói um preço composto semelhante à versão MQL (`(H + Close)/2` em barras de alta, `(L + Close)/2` em barras de baixa, ou `Close` para barras doji).
- O preço composto é passado pelo método de suavização solicitado. Os métodos comuns (SMA, EMA, SMMA/RMA, LWMA e Jurik) estão implementados com indicadores StockSharp. Opções exóticas como T3 ou VIDYA recorrem a uma EMA porque o StockSharp não expõe equivalentes diretos. O parâmetro de phase é mantido para compatibilidade de configuração, mesmo quando o indicador subjacente o ignora.
- A "cor" do Color XMUV é reconstruída comparando o último valor suavizado com o anterior. Inclinações ascendentes são mapeadas para a cor de alta, inclinações descendentes para cor de baixa e valores inalterados para cor neutra.
- `SignalBar` define quantas barras completamente terminadas verificar ao avaliar um sinal (por exemplo, o valor padrão de 1 significa que a lógica aguarda confirmação na barra anterior à mais recente).
- Um flip de alta (cor anterior não de alta, cor atual de alta) fecha qualquer posição vendida e opcionalmente abre ou adiciona a uma posição comprada. Um flip de baixa realiza as ações simétricas para operações vendidas.
- O filtro de tempo imita o EA original: fora da janela de trading a estratégia fecha imediatamente as posições existentes e ignora novas entradas. O filtro suporta sessões noturnas (hora de início posterior à hora de fim).
- `StopLossPoints` e `TakeProfitPoints` são traduzidos em distâncias absolutas usando o passo de preço do instrumento e registrados com `StartProtection` para que o StockSharp gerencie saídas no lado do servidor onde possível.

## Gestão de risco e posições
- As ordens são dimensionadas com o parâmetro `OrderVolume`. Ao inverter a direção, a estratégia adiciona o valor absoluto da posição atual para que a reversão feche a operação antiga e abra uma nova em uma única transação.
- Stop-loss e take-profit opcionais são convertidos de valores de pontos para distâncias de preço absolutas. Defina qualquer parâmetro como zero para desabilitar a respectiva camada de proteção.
- As saídas de posições acionadas pelo flip de cor respeitam os interruptores `EnableBuyExits` e `EnableSellExits`, permitindo controle independente do gerenciamento de comprados e vendidos.

## Parâmetros
- **Candle Type** – Série de candles usada para cálculos (padrão candles de 4 horas).
- **Order Volume** – Tamanho base da ordem de mercado.
- **Enable Long Entries / Enable Short Entries** – Permitir abertura de posições em flips de alta/baixa.
- **Close Longs / Close Shorts** – Habilitar saídas automáticas em transições de cor opostas.
- **Use Time Filter** – Restringir a negociação à sessão configurada.
- **Start Hour / Start Minute / End Hour / End Minute** – Limites da sessão de negociação. Quando o início é posterior ao fim, a sessão se estende pela meia-noite.
- **Smoothing Method** – Algoritmo de média móvel para a linha Color XMUV. As opções sem implementação nativa no StockSharp são substituídas por EMA e estão documentadas acima.
- **Length** – Comprimento de suavização (deve ser positivo).
- **Phase** – Parâmetro de phase auxiliar retido para compatibilidade de configuração.
- **Signal Bar** – Número de barras completadas para atrasar a verificação de sinal. Definir como zero para agir na barra fechada mais recente.
- **Stop Loss (pts) / Take Profit (pts)** – Offsets expressos em pontos de preço; zero desabilita a respectiva camada.

## Notas
- O expert MQL dependia de bibliotecas de suavização externas. Quando tais modos de suavização não estão disponíveis no StockSharp (ParMA, VIDYA, T3) a implementação substitui por uma EMA. Documente essas alternativas ao compartilhar a estratégia com usuários.
- A estratégia armazena apenas o histórico mínimo de cores exigido por `SignalBar`, cumprindo a diretriz do repositório que desencoraja a construção de caches de dados personalizados.
