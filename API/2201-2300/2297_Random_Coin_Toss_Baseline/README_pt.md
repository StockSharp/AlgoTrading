# Estratégia de Referência com Lançamento de Moeda Aleatório
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia replica o clássico exemplo GuruTrader onde a direção da negociação é determinada por um lançamento de moeda.
Em cada candle finalizado, se nenhuma posição estiver aberta, um número pseudoaleatório é gerado e tratado como um lançamento de moeda.
Cara abre uma posição comprada enquanto coroa abre uma posição vendida.
Cada negociação aplica distâncias fixas de take-profit e stop-loss medidas em unidades de preço absolutas.

## Parâmetros
- **Take Profit** – distância do preço de entrada para colocar a ordem de take-profit.
- **Stop Loss** – distância do preço de entrada para colocar a ordem de stop-loss.
- **Use Time Seed** – inicializa o gerador aleatório com o horário atual para resultados diferentes em cada execução. Quando desativado, uma semente fixa é usada.
- **Candle Type** – tipo de candles processados pela estratégia.

## Lógica de Negociação
1. Aguardar um candle finalizado.
2. Garantir que a estratégia pode negociar e que nenhuma posição está aberta.
3. Gerar um valor aleatório e escolher a direção com base no lançamento de moeda.
4. Proteger a posição com as distâncias predefinidas de stop-loss e take-profit.

**Aviso:** Esta estratégia tem fins educacionais apenas e nunca deve ser usada em contas reais.
