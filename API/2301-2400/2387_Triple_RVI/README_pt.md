# Estratégia Triple RVI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia opera usando o **Relative Vigor Index (RVI)** em três períodos de tempo diferentes. As tendências de RVI de longo prazo atuam como filtros, enquanto o menor período é usado para entradas. Uma posição comprada é aberta quando o RVI de curto prazo cruza abaixo de sua linha de sinal enquanto ambos os períodos superiores permanecem altistas. Uma posição vendida é aberta quando o RVI de curto prazo cruza acima de sua linha de sinal e ambos os períodos superiores são baixistas. As posições são fechadas quando qualquer período indica uma mudança de tendência contra a posição atual.

## Parâmetros
- **RviPeriod** – período para cálculo do RVI.
- **CandleType1** – período do filtro RVI mais alto.
- **CandleType2** – período do filtro RVI intermediário.
- **CandleType3** – período de negociação onde os sinais de entrada são gerados.
- **Volume** – tamanho da ordem para ordens de mercado.

## Notas
- Apenas candles fechados são processados.
- A estratégia usa a API de alto nível do StockSharp.
- Os períodos padrão correspondem a candles de 30, 15 e 5 minutos.
