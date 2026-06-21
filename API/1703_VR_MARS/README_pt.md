# Estratégia VR MARS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta amostra demonstra uma versão simplificada do painel de negociação manual **VR---MARS-EN** portado do MQL4 para o StockSharp.

O script original fornecia cinco tamanhos de lote predefinidos e botões para enviar ordens de compra ou venda. Também exibia múltiplos rótulos com estatísticas de negociação. Nesta versão em C# o painel visual é removido, enquanto a ideia central de selecionar um dos cinco tamanhos de lote e executar uma ordem de mercado é preservada.

## Parâmetros

- `Lot1` – tamanho do primeiro lote.
- `Lot2` – tamanho do segundo lote.
- `Lot3` – tamanho do terceiro lote.
- `Lot4` – tamanho do quarto lote.
- `Lot5` – tamanho do quinto lote.
- `SelectedLot` – número de 1 a 5 especificando qual tamanho de lote será usado.
- `Buy` – quando `true`, uma ordem de compra de mercado é enviada ao iniciar a estratégia.
- `Sell` – quando `true`, uma ordem de venda de mercado é enviada ao iniciar a estratégia.

Apenas um dos sinalizadores de direção deve ser habilitado por vez. Quando a estratégia inicia, ela ativa a proteção de posição e envia a ordem de mercado correspondente usando métodos auxiliares de alto nível.

## Notas

Esta estratégia é destinada a fins educacionais e não implementa nenhuma lógica de negociação além da execução imediata de ordens.
