# Estratégia BB Squeeze
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia monitora a contração e expansão das Bandas de Bollinger para explorar rompimentos de volatilidade. Define um squeeze como um período em que a distância entre as bandas superior e inferior se torna estreita em relação à banda do meio. Uma vez que a volatilidade se expande e o preço fecha fora da banda após um squeeze, o sistema entra na direção do rompimento.

As posições são abertas com ordens a mercado. Uma posição comprada é criada quando o preço fecha acima da banda superior após um squeeze, enquanto uma posição vendida é aberta quando o preço fecha abaixo da banda inferior. Apenas velas completadas são processadas, evitando sinais prematuros durante a formação.

O algoritmo rastreia mudanças na largura das bandas sem armazenar históricos completos de velas. Ao comparar a largura atual com a anterior, garante que uma expansão real ocorra antes de colocar ordens. Isso evita entradas durante fases prolongadas de baixa volatilidade onde nenhum rompimento se desenvolve.

Os parâmetros padrão usam uma Banda de Bollinger de 20 períodos com um multiplicador de largura de 2. O limiar de squeeze é definido em 0.05, o que significa que as bandas devem estar dentro de cinco por cento da linha do meio para registrar baixa volatilidade. O período da vela e todos os valores numéricos são totalmente configuráveis e suportam otimização no ambiente StockSharp.
