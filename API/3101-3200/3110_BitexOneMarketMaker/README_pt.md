# Estratégia de Formação de Mercado BitexOne
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
A **Estratégia de Formação de Mercado BitexOne** reproduz o robô de cotação assíncrono do código fonte original
`BITEX.ONE MarketMaker.mq5`. O algoritmo coloca continuamente pares de ordens limite em torno de um preço de referência e
mantém um número igual de níveis nos lados de oferta e demanda. A estratégia foi reescrita para StockSharp usando a API de
alto nível: o gerenciamento de cotações é impulsionado por assinaturas do livro de ordens e nível 1, enquanto a
normalização de risco e volume depende dos metadados do instrumento (`PriceStep`, `VolumeStep` e `MinVolume`).

## Lógica de Trading
1. Determinar o *preço líder* do `PriceSource` selecionado. Por padrão a estratégia espera preços mark, mas pode usar o
   livro de ordens principal ou um instrumento auxiliar (índice ou símbolo mark) via o parâmetro `LeadSecurity`.
2. Calcular a distância entre níveis de preço como `ShiftCoefficient * lead price` e criar uma escada simétrica de cotações
   acima e abaixo da referência.
3. Limitar a exposição total em cada lado a `MaxVolumePerLevel * LevelCount`. As operações executadas reduzem imediatamente
   o volume disponível para que a grade sempre reflita a posição atual.
4. Normalizar preços e volumes usando o tamanho de tick do instrumento e o passo de volume. O algoritmo cancela ordens
   desatualizadas e registra novas quando o preço ou volume derivam além da tolerância herdada do código MQL original
   (limiar de preço de 0,05% e limiar de volume de meio passo).
5. Todas as ordens ativas são canceladas durante eventos de stop/reset para manter o livro limpo.

## Parâmetros
- `MaxVolumePerLevel` – volume máximo cotado em qualquer nível de preço único. Afeta ambos os lados do livro e age como
  limite quando a posição atual cresce.
- `ShiftCoefficient` – offset relativo do preço líder aplicado para cada nível incremental (`leadPrice ± shift * levelIndex`).
- `LevelCount` – número de níveis de cotação por lado. Cada nível cria uma ordem limite de compra e uma de venda.
- `PriceSource` – valor enumerado (`OrderBook`, `MarkPrice`, `IndexPrice`) definindo de onde o preço de referência se origina.
- `LeadSecurity` – instrumento opcional usado quando preços mark ou de índice externos são necessários. Se omitido, o
  instrumento de estratégia principal fornece a referência.

## Notas de Conversão
- O gerenciamento assíncrono de ordens do MetaTrader (SendAsync/ModifyAsync/RemoveOrderAsync) é mapeado para os helpers
  `BuyLimit`/`SellLimit` do StockSharp combinados com cancelamento explícito quando as tolerâncias são excedidas.
- A lógica de balanceamento de posição (`max_pos * level_count ± position`) é preservada para manter a escada centralizada
  e consciente do risco.
- A seleção do preço líder imita a lógica de sufixos do robô original (`symbol`, `symbolm`, `symboli`) permitindo um
  `LeadSecurity` personalizado combinado com uma dica `PriceSource`.
- As verificações periódicas impulsionadas por temporizador em MQL são substituídas por atualizações reativas acionadas por
  mensagens do livro de ordens/nível 1 e eventos de portfólio.

## Notas de Uso
- Certifique-se de que o adaptador conectado fornece profundidade de mercado ou dados de nível 1 tanto para o símbolo de
  trading quanto para o `LeadSecurity` opcional.
- Quando usar feeds mark ou de índice, assine os instrumentos correspondentes antes de iniciar a estratégia para que o
  preço líder fique disponível imediatamente.
- Considere habilitar proteção de portfólio ou gerenciamento de risco adicional no ambiente de hospedagem se a exchange
  exigir proporções rígidas de cotação para operação.
- A estratégia não começa a cotar até que um preço líder positivo seja recebido; verifique a conectividade se nenhuma
  ordem aparecer após a inicialização.
