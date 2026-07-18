# Estratégia de Alcista e Bajista Engulfing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia replica a configuração clássica de velas de engulfing altista e baixista que foi originalmente implementada no MetaTrader para o assessor especializado "Bullish and Bearish Engulfing". O port do StockSharp avalia velas concluídas em um período configurável, opcionalmente ignora um número de barras recentes e reage quando um padrão engulfing atende a um filtro de distância mínima. A lógica foi projetada para traders discricionários que desejam automatizar um padrão de ação do preço bem estabelecido, mantendo o controle sobre direção, volume e como as posições existentes são tratadas.

## Definição do padrão
Um sinal engulfing é confirmado quando duas velas concluídas consecutivas satisfazem as seguintes regras (após aplicar o deslocamento configurado):

- **Engulfing altista**
  - A vela avaliada mais recente fecha acima de sua abertura (corpo altista).
  - A vela anterior fecha abaixo de sua abertura (corpo baixista).
  - A vela altista tem um máximo mais alto e um mínimo mais baixo do que a vela anterior pelo menos pelo filtro de distância.
  - O fechamento altista termina acima da abertura anterior e sua abertura está abaixo do fechamento anterior, respeitando também o filtro de distância.
- **Engulfing baixista**
  - A vela avaliada fecha abaixo de sua abertura (corpo baixista).
  - A vela anterior fecha acima de sua abertura (corpo altista).
  - A vela baixista ainda registra um máximo mais alto, mas fecha bem abaixo da abertura anterior, e sua abertura excede o fechamento anterior, cada um pelo filtro de distância.
  - O mínimo da barra baixista está abaixo do mínimo anterior pelo filtro de distância.

Essas condições reproduzem a implementação original do MetaTrader, que exigia que a vela engulfing cobrisse completamente o corpo anterior e se estendesse além de ambos os extremos. O filtro de distância é medido em pips e convertido para preço usando o passo de preço e as casas decimais do instrumento (cotações forex de 5 e 3 dígitos são automaticamente dimensionadas para pips de 10 pontos).

## Lógica de trading
1. Subscrever ao tipo de vela selecionado por meio da API de alto nível e processar apenas velas concluídas.
2. Manter um pequeno buffer rotativo que armazena os valores OHLC necessários para o valor de deslocamento atual.
3. Quando pelo menos duas velas históricas estiverem disponíveis para avaliação, testar as condições de engulfing altista e baixista descritas acima.
4. Mediante um sinal altista, enviar uma ordem de mercado no lado definido por **BullishSide**. Mediante um sinal baixista, usar o lado configurado via **BearishSide**.
5. Se **CloseOppositePositions** estiver habilitado e existir uma exposição oposta, a estratégia aumenta o volume da ordem pela posição atual absoluta para que a operação resultante feche o lado oposto e abra um novo na direção desejada. Quando o sinalizador está desabilitado, os sinais são ignorados enquanto uma posição oposta estiver aberta.
6. O dimensionamento de posições é controlado pelo parâmetro **Volume** da estratégia (padrão: 1 contrato/lote). Nenhum stop-loss ou take-profit automático é anexado por padrão; a gestão de risco fica a cargo do usuário final ou de módulos de proteção (pode ser combinado com as proteções integradas do StockSharp).

## Parâmetros
| Parâmetro | Descrição | Padrão | Notas |
|-----------|-----------|--------|-------|
| `CandleType` | Período (StockSharp `DataType`) usado para detecção de sinal. | Período de 1 hora | Ajustável para qualquer tipo de vela suportado. |
| `Shift` | Número de velas concluídas a ignorar antes de avaliar o padrão. | 1 | Configurar 1 analisa a última barra fechada; valores mais altos olham mais para trás. |
| `DistanceInPips` | Distância mínima em pips que a vela engulfing deve exceder em relação à anterior. | 0 | Convertido para preço usando o passo de preço do instrumento; útil para filtrar velas com corpos pequenos. |
| `CloseOppositePositions` | Se deve fechar uma posição oposta existente quando um novo sinal é disparado. | `true` | Desabilitar ignora a operação se a exposição atual entrar em conflito com o sinal. |
| `BullishSide` | Lado da ordem executado em um sinal de engulfing altista. | `Buy` | Pode ser invertido para `Sell` para comportamento contrário. |
| `BearishSide` | Lado da ordem executado em um sinal de engulfing baixista. | `Sell` | Pode ser invertido para `Buy` para operar configurações contra a tendência. |
| `Volume` | Tamanho base da ordem. | 1 | O volume da ordem é aumentado em `abs(Position)` ao fechar o lado oposto. |

## Gestão de posições e risco
- Como as ordens são enviadas a mercado sem stops de proteção, combine a estratégia com módulos adicionais (p. ex., `StartProtection`) ou configure controles de risco externos.
- O código original do MetaTrader dimensionava as operações por meio de um gerenciador de capital baseado em risco. Neste port o dimensionamento é simplificado para um parâmetro de volume direto para que o comportamento seja determinístico dentro do StockSharp; integre um bloco personalizado de gerenciamento de capital se o dimensionamento dinâmico for necessário.
- Quando `CloseOppositePositions` é `true`, as reversões são imediatas: o volume da operação é igual ao volume base mais a posição aberta absoluta, garantindo uma transição de plano para nova direção.

## Arquivos
- `CS/BullishBearishEngulfingStrategy.cs` – implementação principal em C# construída sobre a API de estratégia de alto nível do StockSharp.

> **Nota:** Nenhuma implementação Python é fornecida para este ID; apenas a versão C# está incluída conforme solicitado.
