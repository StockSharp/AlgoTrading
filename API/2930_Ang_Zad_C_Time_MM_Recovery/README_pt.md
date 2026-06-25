# Estratégia Ang Zad C Time MM Recovery
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
A Estratégia Ang Zad C Time MM Recovery é uma porta em C# do consultor especialista MetaTrader 5 `Exp_Ang_Zad_C_Tm_MMRec`. A estratégia combina o indicador de canal personalizado Ang_Zad_C com um filtro de sessão de trading configurável e um modelo de tamanho de posição adaptativo que reduz o risco após um número configurável de trades perdedores.

## Lógica do indicador
O indicador Ang_Zad_C constrói dois envelopes adaptativos em torno do preço. Cada envelope é atualizado comparando o preço aplicado escolhido da vela atual e da anterior, movendo-se em direção ao novo preço com o fator de suavização **Ki**. As linhas superior e inferior são avaliadas em barras históricas definidas por **Signal Bar** para evitar agir em velas não terminadas.

## Regras de trading
* **Entrada comprada** – Quando a linha superior estava acima da linha inferior na barra de referência anterior e cruza abaixo ou toca a linha inferior na barra de referência mais recente. Quando isso acontece, qualquer posição vendida aberta é fechada antes de abrir uma nova posição comprada (se habilitado).
* **Entrada vendida** – Quando a linha superior estava abaixo da linha inferior na barra de referência anterior e cruza acima ou toca a linha inferior na barra de referência mais recente. Qualquer posição comprada aberta é fechada antes de abrir uma nova posição vendida (se habilitado).
* **Saída comprada** – Quando a linha superior está abaixo da linha inferior na barra de referência anterior. A saída pode ser desabilitada via **Enable Long Exit**.
* **Saída vendida** – Quando a linha superior está acima da linha inferior na barra de referência anterior. A saída pode ser desabilitada via **Enable Short Exit**.

## Gestão monetária e proteções
* O trading é permitido apenas dentro da janela de tempo configurada quando **Use Time Filter** está habilitado. Posições abertas anteriormente são fechadas assim que a sessão termina.
* O volume do trade é selecionado entre **Normal Volume** e **Small Volume** dependendo de quantos trades perdedores ocorreram para cada lado. Após **Buy Loss Trigger** trades comprados perdedores (ou **Sell Loss Trigger** trades vendidos perdedores) o volume reduzido é usado até que um trade lucrativo resete o contador.
* Níveis opcionais de stop-loss e take-profit são registrados usando distâncias em passos de preço definidas por **Stop Loss Steps** e **Take Profit Steps**.

## Parâmetros
| Nome | Descrição |
| ---- | --------- |
| Candle Type | Período das velas usadas pelo indicador e sinais. |
| Ki | Coeficiente de suavização dos envelopes Ang_Zad_C. |
| Applied Price | Qual preço da vela é alimentado ao indicador. |
| Signal Bar | Quantas barras atrás são usadas para avaliação de sinais (1 = barra fechada anterior). |
| Use Time Filter / Trade Start / Trade End | Habilitar trading baseado em sessão e definir o horário de início e fim da sessão. |
| Enable Long/Short Entry | Permitir a abertura de novos trades comprados ou vendidos. |
| Enable Long/Short Exit | Permitir que a estratégia feche posições na reversão do indicador. |
| Buy/Sell Loss Trigger | Número de trades perdedores antes de aplicar o volume reduzido. |
| Small Volume / Normal Volume | Tamanhos de ordem usados para risco reduzido e normal. |
| Stop Loss Steps / Take Profit Steps | Distância para ordens protetoras expressa em passos de preço. |

## Notas de conversão
* A lógica segue o código MQL5 original, incluindo as verificações de cruzamento direcional e o comportamento da janela de tempo.
* A gestão monetária adaptativa é implementada rastreando o lucro e prejuízo realizados por direção e mudando para o volume reduzido após o número configurado de perdas.
* Os cálculos do indicador evitam qualquer acesso direto ao buffer e são processados em velas terminadas usando a API de alto nível do StockSharp.
