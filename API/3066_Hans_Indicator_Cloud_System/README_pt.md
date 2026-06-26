# Estratégia Hans Indicator Sistema de Nuvem
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
Esta estratégia porta o expert advisor MQL5 `Exp_Hans_Indicator_Cloud_System` para a API de alto nível do StockSharp. Reproduz os
intervalos de "nuvem" do indicador Hans que dividem cada dia de trading em duas sessões de referência e opera quando o indicador reporta um
rompimento acima ou abaixo desses intervalos dinâmicos. A implementação consome uma série de velas configurável (padrão: M30), processa
apenas velas finalizadas, e espelha a lógica de execução retardada do script original atuando na próxima barra após uma mudança
de cor.

## Recriação do indicador Hans
O indicador original desloca todos os timestamps do fuso horário do broker (`LocalTimeZone`) para um fuso horário alvo (`DestinationTimeZone`).
O port do StockSharp aplica o mesmo offset antes de dividir cada dia em duas sessões:

1. **Sessão 1 (04:00–08:00 hora alvo)** – a estratégia registra a máxima mais alta e a mínima mais baixa de todas as velas que caem dentro
   desta janela. Uma vez que a janela termina, a zona é considerada completa.
2. **Sessão 2 (08:00–12:00 hora alvo)** – o processo se repete para a segunda janela. Quando esta sessão termina, seus valores alto/baixo
   substituem a primeira zona pelo resto do dia.

Um buffer configurável (`PipsForEntry`) expresso em passos de preço é adicionado acima da máxima e abaixo da mínima da zona ativa. O
mapa de cores do indicador é reproduzido da seguinte forma:

- `0` – o fechamento está acima da zona superior e o corpo da vela é de alta.
- `1` – o fechamento está acima da zona superior e o corpo da vela é de baixa.
- `3` – o fechamento está abaixo da zona inferior e o corpo da vela é de alta.
- `4` – o fechamento está abaixo da zona inferior e o corpo da vela é de baixa.
- `2` – sem rompimento (estado neutro).

Esses valores são armazenados para emular as pesquisas de `CopyBuffer` realizadas pelo expert MQL5.

## Lógica de trading
- A estratégia mantém um histórico deslizante de códigos de cores e olha `SignalBar` barras (padrão 1) mais uma barra extra, correspondendo à
  chamada `CopyBuffer(..., SignalBar, 2, ...)` da fonte.
- **Abrir comprado**: a barra mais antiga (`SignalBar + 1`) reporta cor `0` ou `1` e a barra mais recente (`SignalBar`) não está colorida
  `0`/`1`. Qualquer exposição vendida existente é fechada antes de abrir um novo comprado de `TradeVolume` unidades.
- **Abrir vendido**: a barra mais antiga reporta cor `3` ou `4` e a barra mais recente não está colorida `3`/`4`. Qualquer exposição comprada
  existente é nivelada primeiro e então um novo vendido é aberto.
- **Fechar comprado**: sempre que a barra mais antiga está colorida `3` ou `4` e as saídas compradas estão habilitadas.
- **Fechar vendido**: sempre que a barra mais antiga está colorida `0` ou `1` e as saídas vendidas estão habilitadas.

As saídas são processadas antes das entradas exatamente como as funções auxiliares dentro de `TradeAlgorithms.mqh`, garantindo que posições opostas
sejam fechadas antes de emitir novas ordens.

## Parâmetros
- **Tipo de vela** (`CandleType`): período das velas processadas.
- **Barra de sinal** (`SignalBar`): quantas velas finalizadas atrás inspecionar para uma mudança de cor.
- **Fuso horário local** (`LocalTimeZone`): fuso horário do broker/servidor em horas.
- **Fuso horário de destino** (`DestinationTimeZone`): fuso horário alvo que define as janelas de sessão.
- **Buffer de rompimento** (`PipsForEntry`): número de passos de preço adicionados acima/abaixo do intervalo de sessão detectado.
- **Habilitar entradas/saídas compradas** (`BuyPosOpen`, `BuyPosClose`): alternadores para gerenciar posições compradas.
- **Habilitar entradas/saídas vendidas** (`SellPosOpen`, `SellPosClose`): alternadores para gerenciar posições vendidas.
- **Volume de trading** (`TradeVolume`): tamanho da ordem usada para cada nova posição; também sincronizado com `Strategy.Volume` no início.

## Notas
- A tradução Python é intencionalmente omitida conforme solicitado.
- Os auxiliares de gestão de dinheiro de `TradeAlgorithms.mqh` (modos de margem, dimensionamento dinâmico de posição, colocação de stop-loss/take-profit)
  são simplificados para um volume de trading fixo e regras de saída explícitas.
- Quando o ativo não expõe `PriceStep`, o buffer de rompimento é interpretado como unidades de preço absolutas, correspondendo à melhor
  aproximação disponível sem informações sobre o tamanho do tick.
