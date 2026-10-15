# Estratégia de Reversão de Três Velas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é um port fiel do StockSharp do consultor especialista MQL5 `Exp_ThreeCandles`. Procura uma reversão clássica de três velas:

1. Duas velas consecutivas em uma direção.
2. Uma terceira vela que inverte a direção e fecha além da barra do meio.
3. Confirmação opcional de volume, a menos que a barra mais antiga no padrão seja excepcionalmente grande.

Quando uma configuração altista aparece, o algoritmo fecha a exposição comprada e pode entrar em uma posição comprada. Uma configuração baixista faz o oposto. Níveis protetores de stop loss e take profit são aplicados usando o passo de preço atual do instrumento.

## Detecção do padrão

A estratégia mantém uma janela deslizante das `SignalBar + 3` velas terminadas mais recentes. A cada nova barra ela verifica a vela no deslocamento `SignalBar` (padrão: 1 barra atrás) e as três velas mais antigas:

- **Reversão altista** (potencial compra):
  - As duas velas mais antigas (`SignalBar + 3` e `SignalBar + 2`) são baixistas.
  - A vela do meio fecha acima da mínima da barra mais antiga.
  - A vela mais recente antes do sinal (`SignalBar + 1`) é altista e fecha acima da abertura da vela do meio.
- **Reversão baixista** (potencial venda):
  - Lógica espelho do caso altista.

Um filtro de volume espelha o indicador original. O filtro é ignorado quando `MaxBarSize` (em passos de preço) é excedido pelo intervalo da vela mais antiga ou quando `VolumeFilter` está definido como `None`. Caso contrário, a reversão deve satisfazer `volume antigo < volume médio` **OU** `volume recente > volume médio` **OU** `volume recente > volume mais antigo`. Volume de tick e real são mapeados ao volume agregado da vela porque o StockSharp não distingue os dois no fluxo de velas de alto nível.

## Gestão de operações

- Se `AllowSellExit` estiver habilitado, um padrão altista cobre imediatamente qualquer posição vendida antes de considerar uma entrada comprada. `AllowBuyExit` se comporta da mesma forma para posições compradas em padrões baixistas.
- Novas posições só são abertas quando a posição atual está zerada e a bandeira `Allow*Entry` correspondente é verdadeira. O tamanho da ordem usa as configurações de volume padrão da estratégia.
- As distâncias de stop loss e take profit (`StopLossPips`, `TakeProfitPips`) são expressas em passos de preço e monitoradas em cada vela terminada.
- O último tempo de sinal altista/baixista processado é armazenado em cache para evitar ações duplicadas enquanto uma vela continua gerando ticks.

## Parâmetros

| Nome | Padrão | Descrição |
| ---- | ------- | ----------- |
| `CandleType` | Período de 4 horas | Série de velas processada pela estratégia. |
| `SignalBar` | 1 | Quantas barras atrás o sinal é avaliado. Deve ser ≥ 0. |
| `MaxBarSize` | 300 | Se o intervalo da barra mais antiga (convertido com `PriceStep`) exceder este valor, o filtro de volume é ignorado. Definir como 0 para sempre ignorar. |
| `VolumeFilter` | `Tick` | Modo de volume (`Tick`, `Real` ou `None`). Tanto `Tick` quanto `Real` usam `TotalVolume` das velas. |
| `AllowBuyEntry` | `true` | Habilitar entradas compradas em padrões altistas. |
| `AllowSellEntry` | `true` | Habilitar entradas vendidas em padrões baixistas. |
| `AllowBuyExit` | `true` | Permitir fechar posições compradas em padrões baixistas. |
| `AllowSellExit` | `true` | Permitir fechar posições vendidas em padrões altistas. |
| `StopLossPips` | 1000 | Distância de stop loss em passos de preço (0 desabilita). |
| `TakeProfitPips` | 2000 | Distância de take profit em passos de preço (0 desabilita). |

## Notas de conversão

- As rotinas de gerenciamento de dinheiro do arquivo de inclusão original MQL5 foram substituídas por chamadas `BuyMarket`/`SellMarket` do StockSharp. O tamanho da posição, portanto, segue o volume padrão do motor.
- O timing do sinal espelha o consultor especialista avaliando a barra no deslocamento `SignalBar` e mantendo o timestamp do sinal anterior.
- Alertas de e-mail, push e som do indicador MQL são intencionalmente omitidos.
- Os modos de volume são preservados, mas ambos são mapeados ao volume agregado da vela porque volumes de tick e real separados não estão disponíveis na API de alto nível.
- Todos os comentários foram reescritos em inglês conforme exigido pelas diretrizes do projeto.

Esta implementação fica próxima ao comportamento original enquanto adere ao modelo de subscrição de alto nível do StockSharp.
