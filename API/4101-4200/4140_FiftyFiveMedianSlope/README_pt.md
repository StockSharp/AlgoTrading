# FiftyFiveMedianSlopeEstratégia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Origem
- Convertido do consultor especialista MetaTrader 4 **55_MA_med_FIN.mq4**.
- Concentra-se na inclinação de uma média móvel de 55 períodos calculada com base nos preços médios das velas.

## Lógica de negociação
- Assina a série de velas configuradas (padrão: período de 1 hora) e processa apenas velas concluídas.
- Calcula uma média móvel sobre o preço mediano (\((High + Low) / 2\)) usando o método selecionado (SMA, EMA, SMMA ou LWMA).
- Armazena os últimos valores da média móvel em um buffer circular para comparar o valor de uma barra atrás com o valor de `MaShift` barras atrás.
- Quando o valor de uma barra atrás é maior que o valor de `MaShift` barras atrás, a estratégia:
  - Fecha primeiro qualquer exposição curta.
  - Abre uma posição longa se o limite `MaxOrders` não for atingido.
- Quando o valor de uma barra atrás é menor que o valor de `MaShift` barras atrás, isso reflete o comportamento das posições curtas.
- Os sinais são alternados por meio de sinalizadores internos para que a estratégia aguarde um cruzamento oposto antes de entrar novamente na mesma direção.
- A negociação é permitida apenas enquanto o horário de abertura da vela satisfaz `StartHour < hour < EndHour`. Os limites são exclusivos para corresponder à implementação MQL original.

## Dimensionamento de posição e gerenciamento de riscos
- `FixedVolume` define o tamanho do lote por ordem de mercado. Quando definida como zero, a estratégia muda para dimensionamento baseado em risco usando `RiskPercentage` e o valor atual do portfólio.
- `MaxOrders` limita quantas vezes o volume base pode ser empilhado na mesma direção. Um valor zero remove o limite.
- `StopLossPoints` e `TakeProfitPoints` opcionais recriam as distâncias de stop-loss e take-profit MT4 por meio de `StartProtection` usando etapas de preço.

## Parâmetros
- `FixedVolume` – tamanho do lote primário. Defina como zero para ativar o dimensionamento baseado em porcentagem.
- `RiskPercentage` – fração da carteira alocada quando `FixedVolume` é igual a zero.
- `TakeProfitPoints` / `StopLossPoints` – distâncias de proteção expressas em etapas de preço.
- `MaPeriod` – comprimento da média móvel mediana (padrão 55).
- `MaShift` – número de barras entre os instantâneos de média móvel recente e histórico (padrão 13).
- `MaMethod` – tipo de cálculo de média móvel (Simples, Exponencial, Suavizado, LinearWeighted).
- `StartHour` / `EndHour` – janela de negociação exclusiva no horário da plataforma (0–23 horas).
- `MaxOrders` – máximo de entradas simultâneas por sentido.
- `CandleType` – período de tempo usado para as velas de sinalização.

## Notas de uso
- Certifique-se de que o instrumento assinado forneça `PriceStep` diferente de zero e metadados de volume para que o alinhamento do volume corresponda aos requisitos de troca.
- O dimensionamento baseado em risco utiliza o valor atual do portfólio e o último preço de fechamento. Se algum deles não estiver disponível, a estratégia volta ao volume zero (sem negociação).
- A estratégia cancela a exposição oposta antes de abrir uma nova posição, emulando o comportamento original do MT4 de fechar ordens opostas.
